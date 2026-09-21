// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using Nag0mi.Common.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.UI.HotKey;
using FcsActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;

namespace Nag0mi.Common.UI;

// Nag0miUI 风格热键面板：窗口外壳（NoBackground + 窗口自身绘制列表圆角底 +
// 每帧按网格精确尺寸），图标/冷却/充能/队列待发用宿主公开件渲染
// （IHotkey/ActionHotkey/DelegateHotkey + IconHelper/ActionHelper/HotkeyQueueManager）;
// 冷却/充能进度为圆圈进度条 + 居中秒数, 充能数/目标角标/激活金框用 UI\ 贴图
// （Charge0-3 / Num2-8 / 062xxx / activeaction, 位于宿主配置目录\ACR\作者\UI\, 缺失退化文字）。
// 右键按住实时交换排序（SwapOrderHelper）, 松手写回 Nag0miUISettings.HotkeyOrder 并落盘;
// 左键按住窗口内任意位置（格子或缝隙）拖动整个面板——格子上的点击与拖动按位移阈值区分,
// 超阈值转拖动后松手不触发热键, 位置落盘。
public sealed class Nag0miUIHotkeyPanelWindow : Window
{
    // 格子圆角
    private const float 圆角 = 8f;

    private readonly IReadOnlyList<(string Name, IHotkey Hotkey, uint GameIcon, bool GameIconHQ)> entries;
    private readonly int columns;
    private readonly float tile;
    private readonly float spacing;

    // 右键拖拽实时交换状态（面板级, 一次只拖一格; 左键点击执行热键与拖拽完全解耦）
    private int 拖拽源 = -1;
    private Vector2 拖拽偏移;
    private List<string>? 拖拽序列;   // 拖拽期间的工作顺序（名字序）, 越过槽位中心即交换
    private bool 拖拽有交换;

    // 格子视觉位置（按热键名索引, 窗口相对坐标, 拖动面板时按钮随窗刚性平移）：
    // 交换让位/松手回落时指数阻尼收敛到格位
    private readonly Dictionary<string, Vector2> 动画格位 = new(StringComparer.Ordinal);

    private bool 位置已恢复;
    private bool 面板拖动中;

    // 自定义热键名 → 目标类型（角标数据源; 设置变更会重建本窗口, 与设置保持一致）
    private readonly Dictionary<string, CustomHotkeyTarget> 目标角标表 = new(StringComparer.Ordinal);
    // 左键按压状态：起点在本窗内的按压才允许转拖动（防从别的窗口按住拖过本窗时劫持）;
    // 按压后移动超阈值即转拖动面板，松手当帧的格子点击被抑制（不触发热键）。
    // 拖动期间不再要求悬停——快速甩动鼠标逃出窗口一帧也不会断拖。
    private bool 左键按压在本窗;
    private bool 按压已转拖动;

    // 参数 tile: 按钮边长（已含缩放）
    // 参数 spacing: 按钮间距(px)
    public Nag0miUIHotkeyPanelWindow(IReadOnlyList<(string Name, IHotkey Hotkey, uint GameIcon, bool GameIconHQ)> entries,
        int columns, float tile, float spacing)
        : base($"{Nag0miUIJobEnv.作者}{Nag0miUIJobEnv.JobName}##Nag0miUI.hotkey",
            ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoBackground   // ImGui 的 WindowBg/边框不画，背景全由 DrawWindowChrome 接管
            | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoFocusOnAppearing)
    {
        this.entries = entries;
        this.columns = Math.Max(1, columns);
        this.tile = MathF.Max(24f, tile);
        this.spacing = Math.Clamp(spacing, 0f, 20f);
        foreach (var c in Nag0miUISettings.Instance.CustomHotkeys)
            目标角标表.TryAdd(c.Name, c.Target);
        RespectCloseHotkey = false;
        AllowBackgroundBlur = false;
        DisableWindowSounds = true;
        IsOpen = true;
    }

    public override void PreDraw()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, SimplePalette.TextPrimary);
        ImGui.PushStyleColor(ImGuiCol.Border, SimplePalette.Border);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 12f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(14f, 12f));
        base.PreDraw();
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
        base.PostDraw();
    }

    public override void Draw()
    {
        var pad = ImGui.GetStyle().WindowPadding;
        var cols = Math.Min(columns, entries.Count);
        var rows = (entries.Count + cols - 1) / cols;
        var size = new Vector2(
            cols * tile + (cols - 1) * spacing + pad.X * 2f,
            rows * tile + (rows - 1) * spacing + pad.Y * 2f);
        // 每帧显式尺寸精确贴合网格（勿改回 AlwaysAutoResize，实测会多出透明边）
        ImGui.SetWindowSize(size, ImGuiCond.Always);

        if (!位置已恢复)
        {
            if (Nag0miUISettings.Instance.热键面板位置 is { } saved)
                ImGui.SetWindowPos(限制到屏幕内(saved));
            位置已恢复 = true;
        }

        DrawWindowChrome();

        // 按用户自定义顺序排列（拖拽调整; 未调整过 = 注册原序）;
        // 拖拽期间改用工作序列（拖拽内已发生的实时交换）
        var ordered = 按自定义顺序排序(entries);
        if (拖拽源 >= 0 && (拖拽序列 == null || 拖拽序列.Count != ordered.Count))
            拖拽序列 = ordered.Select(e => e.Name).ToList();
        if (拖拽序列 != null)
        {
            var byName = new Dictionary<string, (string Name, IHotkey Hotkey, uint GameIcon, bool GameIconHQ)>(ordered.Count);
            foreach (var e in ordered) byName[e.Name] = e;
            ordered = 拖拽序列.Where(byName.ContainsKey).Select(k => byName[k]).ToList();
        }

        var count = ordered.Count;
        if (count == 0)
        {
            HandleWindowDrag();
            return;
        }

        var winPos = ImGui.GetWindowPos();
        var 原点 = winPos + pad;

        // 指针越过另一槽位中心 → 被拖项与该槽位项立即交换（显示顺序与工作序列同步）
        if (拖拽源 >= 0 && 拖拽序列 != null)
        {
            var target = SwapOrderHelper.SlotIndexFromPosition(
                ImGui.GetIO().MousePos, 原点, cols, count, tile, spacing);
            if (target >= 0 && target != 拖拽源)
            {
                SwapOrderHelper.Swap(拖拽序列, 拖拽源, target);
                var byName = new Dictionary<string, (string Name, IHotkey Hotkey, uint GameIcon, bool GameIconHQ)>(ordered.Count);
                foreach (var e in ordered) byName[e.Name] = e;
                ordered = 拖拽序列.Select(k => byName[k]).ToList();
                拖拽源 = target;
                拖拽有交换 = true;
            }
        }

        // 静止格子先画, 拖拽源最后画（跟随鼠标, 不被其它格子遮挡）;
        // 动画位置按窗口相对坐标存储, 绘制时加上窗口原点——拖动面板时按钮随窗刚性平移
        var drawList = ImGui.GetWindowDrawList();
        for (var slot = 0; slot < count; slot++)
        {
            if (slot == 拖拽源) continue;
            var rel = 取动画格位(ordered[slot].Name, 格位偏移(slot));
            DrawTile(drawList, ordered[slot], 原点 + rel, 原点 + rel + new Vector2(tile), slot, floating: false);
        }

        if (拖拽源 >= 0 && 拖拽源 < count)
        {
            var rel = ImGui.GetIO().MousePos - 拖拽偏移 - 原点;
            动画格位[ordered[拖拽源].Name] = rel;   // 记录源格视觉位置（窗口相对）, 松手提交后从该处平滑回落
            DrawTile(drawList, ordered[拖拽源], 原点 + rel, 原点 + rel + new Vector2(tile), 拖拽源, floating: true);
        }

        清理动画格位(ordered);

        if (拖拽源 >= 0)
        {
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
                || !ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                // 松手提交: 写回设置顺序并落盘（拖出面板松手同样提交, 已发生的交换保留）
                if (!Nag0miUISettings.Instance.HotkeyPanelOrderLocked && 拖拽有交换 && 拖拽序列 != null)
                    提交顺序(拖拽序列);
                拖拽源 = -1;
                拖拽序列 = null;
                拖拽有交换 = false;
            }
        }

        HandleWindowDrag();
    }

    // ============================================================
    // === 单个图标按钮（渲染次序：底板 → 图标 → 点击判定 → 状态覆盖层 → 描边） ===
    // ============================================================
    // 参数 floating: 拖拽源格：只画视觉、不参与点击判定与悬停反馈（跟随鼠标画在最上层）。
    private void DrawTile(ImDrawListPtr drawList,
        (string Name, IHotkey Hotkey, uint GameIcon, bool GameIconHQ) entry,
        Vector2 min, Vector2 max, int index, bool floating)
    {
        var name = entry.Name;
        var hk = entry.Hotkey;
        var gameIcon = entry.GameIcon;
        var gameIconHQ = entry.GameIconHQ;

        drawList.AddRectFilled(min, max, SimplePalette.ToU32(SimplePalette.FrameBg), 圆角);

        // 图标来源优先级：游戏内原始图标 id（物品等无动作条目，gameIconHQ=true 取 hq/ 子目录的 HQ 品质）
        // → customIconPath → Action 表动作图标
        var tex = gameIcon != 0
            ? Svc.Texture.GetFromGameIcon(new GameIconLookup(gameIcon, itemHq: gameIconHQ)).GetWrapOrDefault(null)
            : hk.CustomIconPath != null ? IconHelper.GetIconFromPath(hk.CustomIconPath)
            : IconHelper.GetActionIcon(hk.ActionId);
        if (tex != null)
            drawList.AddImageRounded(tex.Handle, min + Vector2.One, max - Vector2.One,
                Vector2.Zero, Vector2.One, SimplePalette.ToU32(Vector4.One), MathF.Max(0f, 圆角 - 1f));

        var hovered = false;
        if (!floating)
        {
            ImGui.SetCursorScreenPos(min);
            ImGui.PushID(name);
            var clicked = ImGui.InvisibleButton("##hk", new Vector2(tile));
            ImGui.PopID();
            // 按压已转拖动面板时，松手当帧的格子点击被抑制（不触发热键）
            if (clicked && !按压已转拖动)
            {
                try { hk.OnClick(); }
                catch (Exception e) { Svc.Log.Error($"[{Nag0miUIJobEnv.作者}] 热键 {name} 点击失败: {e.Message}"); }
            }
            hovered = ImGui.IsItemHovered();

            // 起拖: 悬停格 + 右键拖动超过阈值 → 该格为源格（右键不触发按钮, 悬停判定全程可用;
            // 锁定排序时不起拖, 点击执行不受影响）; 记录抓取偏移供源格跟随鼠标
            if (!Nag0miUISettings.Instance.HotkeyPanelOrderLocked && 拖拽源 < 0
                && hovered && ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                拖拽源 = index;
                拖拽偏移 = ImGui.GetIO().MousePos - min;
                拖拽有交换 = false;
            }

            // 拖拽换位/拖动面板进行中抑制悬停反馈, 避免高亮/提示跳动
            if (拖拽源 >= 0 || 按压已转拖动)
                hovered = false;
        }

        // 切换型逻辑按钮（同步镜头/校准正方向）：激活时底部画强调色横条
        var accent = SimplePalette.Accent;
        if (hk is DelegateHotkey dk && dk.IsActive())
            drawList.AddRectFilled(new Vector2(min.X + 7f, max.Y - 3f), new Vector2(max.X - 7f, max.Y - 1f),
                SimplePalette.ToU32(accent), 99f);

        if (hk.ActionId != 0 && HotkeyQueueManager.IsPending(hk.ActionId))
        {
            // 队列待发：强调色呼吸罩 + 加粗描边
            var pulse = 0.72f + 0.18f * MathF.Sin((float)ImGui.GetTime() * 5.5f);
            drawList.AddRectFilled(min, max,
                SimplePalette.ToU32(SimplePalette.WithAlpha(accent, 0.10f * pulse)), 圆角);
            drawList.AddRect(min, max,
                SimplePalette.ToU32(SimplePalette.WithAlpha(accent, 0.9f)), 圆角, ImDrawFlags.RoundCornersAll, 2.2f);
        }
        else if (hk.ActionId != 0)
        {
            DrawCooldown(drawList, hk, min, max);
        }

        // 自定义热键目标角标（画在状态覆盖层之后、描边之前, 不被冷却罩遮挡）
        if (目标角标表.TryGetValue(name, out var 角标目标))
            DrawTargetBadge(drawList, 角标目标, min, max);

        // 描边：平时 BorderStrong（20%），悬停提亮到主文字色 40%
        drawList.AddRect(min, max,
            SimplePalette.ToU32(hovered ? SimplePalette.WithAlpha(SimplePalette.TextPrimary, 0.40f)
                                        : SimplePalette.BorderStrong),
            圆角, ImDrawFlags.RoundCornersAll, 1.2f);

        // 已释放且 buff 未结束：activeaction 金框盖在最上层
        if (hk.ActionId != 0 && 技能激活中(hk.ActionId))
        {
            var activeTex = 取贴图("activeaction.png");
            if (activeTex != null)
                drawList.AddImage(activeTex.Handle, min - new Vector2(1.5f), max + new Vector2(1.5f));
        }

        if (hovered) ImGui.SetTooltip(name);
    }

    // 技能激活判定：技能对应的 buff 仍在生效（见 Nag0miUIJobEnv.HotkeyActiveBuffs 映射）。
    // 自身类 buff 只看自己; 目标型 buff（极光/刚玉之心/石之心）看自己或任意存活队友——
    // 施放后目标可能变化, 按「谁还带着这个 buff」判定最贴近实际表现。
    private static bool 技能激活中(uint actionId)
    {
        if (!Nag0miUIJobEnv.HotkeyActiveBuffs.TryGetValue(actionId, out var map)) return false;
        var me = Core.Me;
        if (me == null) return false;
        if (me.HasStatus(map.BuffId)) return true;
        if (map.SelfOnly) return false;
        foreach (var c in PartyHelper.GetParty())
            if (!c.IsDead && c.HasStatus(map.BuffId)) return true;
        return false;
    }

    // 面板贴图目录：ACR 程序集由宿主按字节流加载（Assembly.Location 为空）,
    // 目录约定为 宿主配置目录\ACR\作者\UI\（与安装包布局一致, csproj 输出目录同构）;
    // Assembly.Location 非空时兜底（开发直跑）。只缓存命中, 未找到时下帧重试。
    private static string? 贴图目录;
    private static bool 贴图目录警告过;

    private static string? 取贴图目录()
    {
        if (贴图目录 != null) return 贴图目录;
        var candidates = new List<string?>(2)
        {
            Svc.PluginInterface.ConfigDirectory?.FullName is { } cfg
                ? Path.Combine(cfg, "ACR", Nag0miUIJobEnv.作者, "UI")
                : null,
            Path.GetDirectoryName(typeof(Nag0miUIHotkeyPanelWindow).Assembly.Location) is { Length: > 0 } asm
                ? Path.Combine(asm, "UI")
                : null,
        };
        foreach (var dir in candidates)
            if (dir != null && Directory.Exists(dir)) { 贴图目录 = dir; return dir; }
        if (!贴图目录警告过)
        {
            贴图目录警告过 = true;
            Svc.Log.Warning($"[{Nag0miUIJobEnv.作者}] 热键面板贴图目录未找到（尝试过: {string.Join(" | ", candidates)}）");
        }
        return null;
    }

    // 贴图缓存持有 ISharedImmediateTexture 共享句柄（保活底层纹理）, 绘制帧才取 wrap。
    // 直接缓存 wrap 不行：句柄被 GC 回收后底层纹理销毁, 缓存的 wrap 变成已销毁对象,
    // 再访问 Handle 抛 ObjectDisposedException; 异步就绪前 GetWrapOrDefault 返回 null, 下帧重试。
    private static readonly Dictionary<string, ISharedImmediateTexture> 贴图缓存 = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> 贴图失败 = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> 贴图警告过 = new(StringComparer.OrdinalIgnoreCase);

    private static IDalamudTextureWrap? 取贴图(string fileName)
    {
        if (贴图失败.Contains(fileName)) return null;
        if (!贴图缓存.TryGetValue(fileName, out var tex))
        {
            var dir = 取贴图目录();
            if (dir == null) return null;
            var path = Path.Combine(dir, fileName);
            if (!File.Exists(path))
            {
                if (贴图警告过.Add(fileName))
                    Svc.Log.Warning($"[{Nag0miUIJobEnv.作者}] 贴图缺失: {path}");
                return null;
            }
            try
            {
                tex = Svc.Texture.GetFromFile(path);
            }
            catch (Exception e)
            {
                贴图失败.Add(fileName);
                if (贴图警告过.Add(fileName))
                    Svc.Log.Warning($"[{Nag0miUIJobEnv.作者}] 贴图加载失败 {fileName}: {e.Message}");
                return null;
            }
            贴图缓存[fileName] = tex;
        }
        return tex.GetWrapOrDefault(null);
    }

    // 冷却/充能进度：整格压暗遮罩 + 圆圈进度条（顶部起, 弧长 = 剩余比例, 随时间顺时针消减）
    // + 左下角秒数。
    // 充能技能的总冷却是「攒满全部充能」的时长（极光清空两层=120s）, 进度与秒数折算成
    // 「下一层充能」的剩余显示, 与游戏内原生表现一致; 充能数角标常驻右下角（Charge0-3.png）,
    // 最后绘制不被进度环盖住, 贴图缺失退化回文字角标。
    private static void DrawCooldown(ImDrawListPtr drawList, IHotkey hk, Vector2 min, Vector2 max)
    {
        var cd = ActionHelper.GetActionCooldown(hk.ActionId);
        var charges = ActionHelper.GetActionCharges(hk.ActionId);
        var maxCharges = 取最大充能(hk.ActionId);

        if (cd > 0f && ActionHelper.IsActionRecharging(cd, charges, maxCharges))
        {
            // 冷却中整格压暗, 让倒计时状态一眼可辨
            drawList.AddRectFilled(min, max, SimplePalette.ToU32(new Vector4(0f, 0f, 0f, 0.45f)), 圆角);

            var recast = ActionHelper.GetActionRecastTime(hk.ActionId);
            if (recast > 0.001f)
            {
                // 单层充能时长 = 总复唱 / 最大充能; 缺失层数 = 最大充能 - 当前整层数
                var perCharge = recast / maxCharges;
                var missing = Math.Max(1, maxCharges - (int)MathF.Floor(charges + 0.001f));
                var next = Math.Clamp(cd - (missing - 1) * perCharge, 0.001f, perCharge);
                var progress = Math.Clamp(next / perCharge, 0f, 1f);

                var center = (min + max) * 0.5f;
                var radius = (max.X - min.X) * 0.5f - 2.5f;
                const float 线宽 = 3f;
                drawList.AddCircle(center, radius,
                    SimplePalette.ToU32(new Vector4(0.1f, 0.1f, 0.1f, 0.55f)), 0, 线宽);
                // 弧尾固定在顶部, 弧头随剩余时间从顶部顺时针回扫（与原生热键栏一致）
                var a0 = -MathF.PI * 0.5f;
                drawList.PathArcTo(center, radius,
                    a0 + (1f - progress) * MathF.PI * 2f, a0 + MathF.PI * 2f, 0);
                drawList.PathStroke(
                    SimplePalette.ToU32(SimplePalette.WithAlpha(SimplePalette.TextPrimary, 0.9f)),
                    ImDrawFlags.None, 线宽);

                if (next > 0.05f)
                {
                    var text = ((int)MathF.Ceiling(next)).ToString();
                    const float fontScale = 1.1f;
                    var fontSize = ImGui.GetFontSize() * fontScale;
                    var ts = ImGui.CalcTextSize(text) * fontScale;
                    var tp = new Vector2(min.X + 3f, max.Y - ts.Y - 2f);
                    drawList.AddText(ImGui.GetFont(), fontSize, tp + new Vector2(1f, 1f), 4278190080u, text);
                    drawList.AddText(ImGui.GetFont(), fontSize, tp, 4294967295u, text);
                }
            }
        }

        if (maxCharges > 1)
        {
            var n = Math.Clamp((int)MathF.Floor(charges + 0.001f), 0, maxCharges);
            var tex = 取贴图($"Charge{Math.Min(n, 3)}.png");
            if (tex != null)
            {
                
                var size = (max - min) * 0.45f;
                drawList.AddImage(tex.Handle, max - size - new Vector2(2f, 1f),
                    max - new Vector2(2f, 1f));
            }
            else
            {
                var nText = n.ToString();
                var nts = ImGui.CalcTextSize(nText);
                var ntp = max - nts - new Vector2(5f, 3f);
                drawList.AddRectFilled(ntp - new Vector2(4f, 1f), max - new Vector2(1f),
                    SimplePalette.ToU32(new Vector4(0.1f, 0.1f, 0.1f, 0.85f)), 5f);
                drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize(), ntp,
                    SimplePalette.ToU32(SimplePalette.TextPrimary), nText);
            }
        }
    }

    // 最大充能数：技能表 MaxCharges 列对特性给层数的技能（如极光88级特性）是 0,
    // 必须走游戏本体函数（按当前等级含特性）; 读取失败退回表值, 保底 1。
    private static unsafe int 取最大充能(uint actionId)
    {
        try
        {
            var n = FcsActionManager.GetMaxCharges(actionId, 0u);
            if (n > 0) return n;
        }
        catch { /* 宿主未就绪 */ }
        return Math.Max(1, ActionHelper.GetMaxCharges(actionId));
    }

    // 目标角标配色（自定义热键）
    private static readonly Vector4 角标蓝 = new(0.30f, 0.55f, 1.00f, 1f);   // 小队数字 / 坦克
    private static readonly Vector4 角标绿 = new(0.30f, 0.85f, 0.45f, 1f);   // 治疗
    private static readonly Vector4 角标红 = new(0.92f, 0.30f, 0.32f, 1f);   // 输出
    private static readonly Vector4 角标黄 = new(1.00f, 0.85f, 0.25f, 1f);   // 不限定职业

    // 数字/职能角标贴图：左上角, 边长 ≈ 格子边长的 1/√3（面积约 1/3）, 
    private const float 角标边长比 = 0.577f;
    private const uint 角标着色 = 0xFFFFFFFFu; // ImGui 0xAABBGGRR, 

    // 自定义热键的目标角标：小队成员2-8 → 左上角 Num2-8.png 数字贴图;
    // 血量最低的队友/坦克/奶妈/输出 → 左上角对应职能贴图（062144/062581/062582/062583）。
    // 贴图缺失退化回文字角标（数字 / HP LOW, 带黑影保可读）。
    private static void DrawTargetBadge(ImDrawListPtr drawList, CustomHotkeyTarget target, Vector2 min, Vector2 max)
    {
        const float fontScale = 0.72f;
        var fontSize = ImGui.GetFontSize() * fontScale;
        var tileH = max.Y - min.Y;

        if (target >= CustomHotkeyTarget.Party2)
        {
            var num = (int)target - (int)CustomHotkeyTarget.Party2 + 2;
            var tex = 取贴图($"Num{num}.png");
            if (tex != null)
            {
                var h = tileH * 角标边长比;
                var w = h * tex.Width / tex.Height;
                var pos = min + new Vector2(2f, 1f);
                drawList.AddImage(tex.Handle, pos, pos + new Vector2(w, h),
                    Vector2.Zero, Vector2.One, 角标着色);
                return;
            }

            var numText = num.ToString();
            var textPos = min + new Vector2(3f, 1f);
            drawList.AddText(ImGui.GetFont(), fontSize, textPos + new Vector2(1f, 1f), 4278190080u, numText);
            drawList.AddText(ImGui.GetFont(), fontSize, textPos, SimplePalette.ToU32(角标蓝), numText);
            return;
        }

        var iconFile = target switch
        {
            CustomHotkeyTarget.LowestHpParty => "062144_hr1.png",
            CustomHotkeyTarget.LowestHpTank => "062581_hr1.png",
            CustomHotkeyTarget.LowestHpHealer => "062582_hr1.png",
            CustomHotkeyTarget.LowestHpDps => "062583_hr1.png",
            _ => (string?)null,
        };
        if (iconFile == null) return;

        var icon = 取贴图(iconFile);
        if (icon != null)
        {
            var size = tileH * 角标边长比;
            var pos = min + Vector2.One;
            drawList.AddImage(icon.Handle, pos, pos + new Vector2(size),
                Vector2.Zero, Vector2.One, 角标着色);
            return;
        }

        var color = target switch
        {
            CustomHotkeyTarget.LowestHpTank => 角标蓝,
            CustomHotkeyTarget.LowestHpHealer => 角标绿,
            CustomHotkeyTarget.LowestHpDps => 角标红,
            _ => 角标黄,
        };
        const string text = "HP LOW";
        var ts = ImGui.CalcTextSize(text) * fontScale;
        var tp = new Vector2((min.X + max.X - ts.X) * 0.5f, min.Y + 1f);
        drawList.AddText(ImGui.GetFont(), fontSize, tp + new Vector2(1f, 1f), 4278190080u, text);
        drawList.AddText(ImGui.GetFont(), fontSize, tp, SimplePalette.ToU32(color), text);
    }

    // ============================================================
    // === 排列顺序与拖拽换位 ===
    // ============================================================
    // 把构建器产出的热键条目按 Nag0miUISettings.HotkeyOrder 的自定义顺序重排:
    // 顺序表里没有的名字按原相对顺序置尾（LINQ OrderBy 稳定排序）。
    private static List<(string Name, IHotkey Hotkey, uint GameIcon, bool GameIconHQ)> 按自定义顺序排序(
        IReadOnlyList<(string Name, IHotkey Hotkey, uint GameIcon, bool GameIconHQ)> entries)
    {
        if (entries.Count < 2) return entries.ToList();
        var order = Nag0miUISettings.Instance.GetOrderedHotkeyNames();
        var rank = new Dictionary<string, int>(order.Count);
        for (var i = 0; i < order.Count; i++) rank[order[i]] = i;
        return entries.OrderBy(e => rank.TryGetValue(e.Name, out var r) ? r : int.MaxValue).ToList();
    }

    // 松手提交：把拖拽后的可见序列合并回完整顺序（隐藏键保持原槽位）, 写回设置并落盘。
    private static void 提交顺序(List<string> 新可见序列)
    {
        var s = Nag0miUISettings.Instance;
        var full = s.GetOrderedHotkeyNames();
        var 可见 = new HashSet<string>(新可见序列, StringComparer.Ordinal);
        var merged = new List<string>(full.Count);
        var vi = 0;
        foreach (var k in full)
            merged.Add(可见.Contains(k) ? 新可见序列[vi++] : k);
        s.HotkeyOrder = merged;
        s.Save();
    }

    // 显示槽位 slot 相对网格原点的偏移。
    private Vector2 格位偏移(int slot)
        => new Vector2(slot % columns, slot / columns) * new Vector2(tile + spacing);

    // 格子视觉位置指数阻尼收敛到目标格位（窗口相对坐标）; 新出现的格子直接落位, 不做飞入动画。
    private Vector2 取动画格位(string name, Vector2 目标)
    {
        if (!动画格位.TryGetValue(name, out var 当前))
        {
            动画格位[name] = 目标;
            return 目标;
        }

        var next = SwapOrderHelper.AnimatePosition(当前, 目标, ImGui.GetIO().DeltaTime);
        动画格位[name] = next;
        return next;
    }

    // 热键清单变化后移除失效条目的动画位置记录。
    private void 清理动画格位(IReadOnlyList<(string Name, IHotkey Hotkey, uint GameIcon, bool GameIconHQ)> entries)
    {
        if (动画格位.Count <= entries.Count) return;
        var live = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries) live.Add(entry.Name);
        foreach (var name in 动画格位.Keys.Where(name => !live.Contains(name)).ToArray())
            动画格位.Remove(name);
    }

    // ============================================================
    // === 外壳与拖动 ===
    // ============================================================
    // 面板底色与边框：画进窗口自身的绘制列表（先垫占位命令，见 Nag0miUILayer），
    // 保证两个窗口重叠时背景仍然盖住身后窗口的内容。
    private void DrawWindowChrome()
    {
        var pos = ImGui.GetWindowPos();
        var max = pos + ImGui.GetWindowSize();
        var drawList = ImGui.GetWindowDrawList();
        Nag0miUILayer.垫牺牲帧(drawList);
        // 覆盖 Begin 压入的内容区内层裁剪, 底色/边框画满全窗口（本窗无标题栏）
        drawList.PushClipRect(pos, max, false);

        var tint = new Vector4(0.11f, 0.11f, 0.12f, 0.85f);

        drawList.AddRectFilled(pos + new Vector2(0.5f), max - new Vector2(0.5f),
            SimplePalette.ToU32(tint), 12f, ImDrawFlags.RoundCornersAll);
        drawList.AddRect(pos + new Vector2(1f), max - new Vector2(1f),
            SimplePalette.ToU32(SimplePalette.Border), 11f, ImDrawFlags.RoundCornersAll, 1f);
        drawList.PopClipRect();
    }

    // 左键按住窗口内任意位置（格子或缝隙）拖动整个面板：格子上的点击与拖动按位移阈值区分，
    // 超过阈值即转拖动、松手不再触发热键；未超阈值松手仍是点击执行。位置钳制在屏幕工作区内，
    // 松手落盘。拖动期间不校验悬停——快速甩动时鼠标一帧逃出窗口也不会断拖停在原地。
    // 本方法在格子绘制之后调用：松手当帧先由 DrawTile 按 按压已转拖动 抑制点击，再到这里
    // 重置按压标志，时序不会错。
    private void HandleWindowDrag()
    {
        // AllowWhenBlockedByActiveItem：按住格子（按钮占住 ActiveId）时悬停判定仍为真
        var hovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            左键按压在本窗 = true;

        if (左键按压在本窗 && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            ImGui.SetWindowPos(限制到屏幕内(ImGui.GetWindowPos() + ImGui.GetIO().MouseDelta));
            面板拖动中 = true;
            按压已转拖动 = true;
        }
        else if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            if (面板拖动中)
            {
                面板拖动中 = false;
                Nag0miUISettings.Instance.热键面板位置 = ImGui.GetWindowPos();
                Nag0miUISettings.Instance.Save();
            }
            左键按压在本窗 = false;
            按压已转拖动 = false;
        }
    }

    private static Vector2 限制到屏幕内(Vector2 pos)
    {
        var vp = ImGui.GetMainViewport();
        var size = ImGui.GetWindowSize();
        if (size.X <= 0f || size.Y <= 0f) size = new Vector2(360f, 70f);
        var min = vp.WorkPos + new Vector2(4f);
        var max = vp.WorkPos + vp.WorkSize - size - new Vector2(4f);
        if (max.X < min.X) max.X = min.X;
        if (max.Y < min.Y) max.Y = min.Y;
        return Vector2.Clamp(pos, min, max);
    }
}
