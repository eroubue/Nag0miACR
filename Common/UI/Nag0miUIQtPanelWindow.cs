// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Nag0mi.Common.Data;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.UI.QuickToggles;

namespace Nag0mi.Common.UI;

// Nag0miUI 风格 QT 面板悬浮窗：图标瓦片网格（启用=彩色图标+绿描边+底部微光条,
// 关闭=35%透明度+深灰罩层+红描边, marker 角标区分同技能多 QT）。
// 左键点击即时切 QT（走 APIHelper.设置QT 含级联规则）; 未锁定时右键按住实时交换排序,
// 松手一次性写回 Nag0miUISettings.QtOrder 并落盘; 左键按住窗口内任意位置（瓦片或缝隙）
// 拖动整个面板——瓦片上的点击与拖动按位移阈值区分, 超阈值转拖动后松手不触发开关, 位置落盘。
public sealed class Nag0miUIQtPanelWindow : Window
{
    // 基准格边长(px, 100% 缩放时; 52.8 = 初版 44px 的 120%, 以 120% 为新基准 100%)
    private const float 基准格 = 52.8f;
    // 瓦片圆角
    private const float 圆角 = 8f;
    // 图标解析失败的退化图标（游戏内问号图标）
    private const uint 问号图标 = 60071u;

    private static int 每行 => Math.Clamp(Nag0miUISettings.Instance.QtPanelColumns, 1, 12);
    private static float 格 => 基准格 * Nag0miUISettings.Instance.QtPanelScalePercent / 100f;
    private static float 间距 => Math.Clamp(Nag0miUISettings.Instance.QtPanelSpacing, 0f, 20f);

    // 右键拖拽实时交换状态（面板级, 一次只拖一格; 左键点击切开关与拖拽完全解耦）
    private int 拖拽源 = -1;
    private Vector2 拖拽偏移;
    private List<string>? 拖拽序列;   // 拖拽期间的工作顺序（键序）, 越过槽位中心即交换
    private bool 拖拽有交换;

    // 格子视觉位置（按 QT 键索引, 窗口相对坐标, 拖动面板时按钮随窗刚性平移）：
    // 交换让位/松手回落时指数阻尼收敛到格位
    private readonly Dictionary<string, Vector2> 动画格位 = new(StringComparer.Ordinal);

    private bool 位置已恢复;
    private bool 面板拖动中;
    // 左键按压状态：起点在本窗内的按压才允许转拖动（防从别的窗口按住拖过本窗时劫持）;
    // 按压后移动超阈值即转拖动面板，松手当帧的瓦片点击被抑制（不触发 QT 开关）
    private bool 左键按压在本窗;
    private bool 按压已转拖动;

    public Nag0miUIQtPanelWindow() : base($"{Nag0miUIJobEnv.作者}{Nag0miUIJobEnv.JobTag} QT面板###Nag0miUI.QtPanel",
        ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoCollapse
        | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoResize      // 去掉右下角缩放 grip 与边缘缩放
        | ImGuiWindowFlags.NoMove        // 标题栏移动禁用, 移动走缝隙左键拖拽
        | ImGuiWindowFlags.NoBackground  // ImGui 的 WindowBg/边框完全不画, 背景全由 DrawWindowChrome 接管
        | ImGuiWindowFlags.NoNav
        | ImGuiWindowFlags.NoFocusOnAppearing)
    {
        RespectCloseHotkey = false;
        AllowBackgroundBlur = false;
        DisableWindowSounds = true;
    }

    public override void PreDraw()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, SimplePalette.TextPrimary);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, SimplePalette.TextDisabled);
        ImGui.PushStyleColor(ImGuiCol.Border, SimplePalette.Border);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 4f);
        // 内边距 16 ≥ 外壳笔触边框 12 + 4px 净距：瓦片不压笔触, 边框有完整展开空间
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16f, 16f));
        base.PreDraw();
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
        base.PostDraw();
    }

    public override void Draw()
        // 自定义字体（设置页「个性化」配置; 未启用/加载失败时用宿主默认字体）
        => WindowFontManager.DrawWithCustomFont(Nag0miUICommonSettings.Instance.Qt面板字体, "QT面板", DrawContent);

    private void DrawContent()
    {
        IReadOnlyList<QtDefinition> defs;
        try
        {
            defs = QtRegistry.ResolveVisible(
                PromeSettings.Instance.QuickToggles.Keys,
                PromeSettings.Instance.HiddenQts);
        }
        catch
        {
            return;
        }

        // 按用户自定义顺序排列（设置页/拖拽调整; 未调整过 = 注册原序）;
        // 拖拽期间改用工作序列（拖拽内已发生的实时交换）
        var ordered = 按自定义顺序排序(defs);
        if (拖拽源 >= 0 && (拖拽序列 == null || 拖拽序列.Count != ordered.Count))
            拖拽序列 = ordered.Select(d => d.Id).ToList();
        if (拖拽序列 != null)
        {
            var byId = new Dictionary<string, QtDefinition>(ordered.Count);
            foreach (var d in ordered) byId[d.Id] = d;
            ordered = 拖拽序列.Where(byId.ContainsKey).Select(k => byId[k]).ToList();
        }

        // 每帧显式设置尺寸，让窗口精确贴合瓦片网格。
        // 不要改回 AlwaysAutoResize：自适应尺寸会被窗口状态持久化干扰，
        // 导致窗口比内容宽出一截、左右多出透明边。
        ImGui.SetWindowSize(计算窗口尺寸(ordered.Count), ImGuiCond.Always);

        if (!位置已恢复)
        {
            if (Nag0miUISettings.Instance.Qt面板位置 is { } saved)
                ImGui.SetWindowPos(限制到屏幕内(saved));
            位置已恢复 = true;
        }

        DrawWindowChrome();

        var count = ordered.Count;
        if (count == 0)
        {
            ImGui.TextDisabled("无可用开关（QT管理里配置显隐）");
            HandleWindowDrag();
            return;
        }

        // 右键拖拽换位: 锁定开关只封拖拽, 点击开关不受影响
        var locked = Nag0miUISettings.Instance.QtPanelOrderLocked;
        var winPos = ImGui.GetWindowPos();
        var 原点 = winPos + ImGui.GetStyle().WindowPadding;

        // 指针越过另一槽位中心 → 被拖项与该槽位项立即交换（显示顺序与工作序列同步）
        if (拖拽源 >= 0 && 拖拽序列 != null)
        {
            var target = SwapOrderHelper.SlotIndexFromPosition(
                ImGui.GetIO().MousePos, 原点, 每行, count, 格, 间距);
            if (target >= 0 && target != 拖拽源)
            {
                SwapOrderHelper.Swap(拖拽序列, 拖拽源, target);
                var byId = new Dictionary<string, QtDefinition>(ordered.Count);
                foreach (var d in ordered) byId[d.Id] = d;
                ordered = 拖拽序列.Select(k => byId[k]).ToList();
                拖拽源 = target;
                拖拽有交换 = true;
            }
        }

        // 静止瓦片先画, 拖拽源最后画（跟随鼠标, 不被其它瓦片遮挡）;
        // 动画位置按窗口相对坐标存储, 绘制时加上窗口原点——拖动面板时瓦片随窗刚性平移
        for (var slot = 0; slot < count; slot++)
        {
            if (slot == 拖拽源) continue;
            var rel = 取动画格位(ordered[slot].Id, 格位偏移(slot));
            画瓦片(ordered[slot], 原点 + rel, locked, slot);
        }

        if (拖拽源 >= 0 && 拖拽源 < count)
        {
            // 跟随鼠标但钳制在网格范围内——瓦片不允许拖出窗口外
            var rel = SwapOrderHelper.ClampToGrid(ImGui.GetIO().MousePos - 拖拽偏移 - 原点, 每行, count, 格, 间距);
            动画格位[ordered[拖拽源].Id] = rel;   // 记录源格视觉位置（窗口相对）, 松手提交后从该处平滑回落
            画瓦片(ordered[拖拽源], 原点 + rel, locked, 拖拽源);
        }

        清理动画格位(ordered);

        if (拖拽源 >= 0)
        {
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
                || !ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                // 松手提交: 写回设置顺序并落盘（拖出面板松手同样提交, 已发生的交换保留）
                if (!locked && 拖拽有交换 && 拖拽序列 != null)
                    提交顺序(拖拽序列);
                拖拽源 = -1;
                拖拽序列 = null;
                拖拽有交换 = false;
            }
        }

        HandleWindowDrag();
    }

    // ============================================================
    // === 单个 QT 瓦片 ===
    // ============================================================
    // 渲染次序：图标（按开关调透明度）→ marker 角标 → 关闭罩层 → 状态描边/微光 → 交互判定。
    private void 画瓦片(QtDefinition def, Vector2 min, bool locked, int index)
    {
        var on = PromeSettings.Instance.GetQt(def.Id);
        var max = min + new Vector2(格);
        var drawList = ImGui.GetWindowDrawList();
        var isDraggingSource = index == 拖拽源;

        // 图标（解析器返回 null/0 → 问号图标 + QT 显示名首字角标）
        var (iconId, isGameIcon, marker) = 解析图标(def);
        var tex = isGameIcon ? IconHelper.GetGameIcon(iconId) : IconHelper.GetActionIcon(iconId);
        var imageMin = min + new Vector2(2f);
        var imageMax = max - new Vector2(2f);
        if (tex != null)
            drawList.AddImageRounded(tex.Handle, imageMin, imageMax, Vector2.Zero, Vector2.One,
                SimplePalette.ToU32(new Vector4(1f, 1f, 1f, on ? 1f : 0.35f)),
                MathF.Max(0f, 圆角 - 1.5f));

        // marker 角标：图标内底部深色条 + 文字（区分同技能多 QT）
        if (!string.IsNullOrEmpty(marker))
        {
            var font = ImGui.GetFont();
            var fontSize = 格 * 0.30f;
            var stripHeight = fontSize * 1.3f;
            var stripMin = new Vector2(imageMin.X, imageMax.Y - stripHeight);
            drawList.AddRectFilled(stripMin, imageMax,
                SimplePalette.ToU32(new Vector4(0.04f, 0.04f, 0.06f, 0.80f)),
                MathF.Max(0f, 圆角 - 1.5f), ImDrawFlags.RoundCornersBottom);
            var textSize = ImGui.CalcTextSize(marker) * (fontSize / ImGui.GetFontSize());
            var textPos = stripMin + (new Vector2(imageMax.X - imageMin.X, stripHeight) - textSize) * 0.5f;
            drawList.AddText(font, fontSize, textPos,
                SimplePalette.ToU32(new Vector4(1f, 1f, 1f, on ? 1f : 0.55f)), marker);
        }

        // 关闭：深灰半透明罩层
        if (!on)
            drawList.AddRectFilled(imageMin, imageMax,
                SimplePalette.ToU32(SimplePalette.QtOffVeil), MathF.Max(0f, 圆角 - 1.5f));

        // 交互判定（拖拽源跟随鼠标时仍占住原槽位的悬停, 但不再响应点击/起拖）
        ImGui.SetCursorScreenPos(min);
        ImGui.PushID(def.Id);
        var clicked = ImGui.InvisibleButton("##qt", new Vector2(格));
        ImGui.PopID();
        var hovered = ImGui.IsItemHovered();

        // 按压已转拖动面板时，松手当帧的按钮点击被抑制（不触发开关）
        if (clicked && !isDraggingSource && !按压已转拖动)
            APIHelper.设置QT(def.Id, !on);   // 左键即时生效（含级联规则）

        // 起拖: 悬停格 + 右键拖动超过阈值 → 该格为源格（右键不触发按钮 Active, 悬停判定全程可用）
        if (!locked && 拖拽源 < 0 && hovered && ImGui.IsMouseDragging(ImGuiMouseButton.Right))
        {
            拖拽源 = index;
            拖拽偏移 = ImGui.GetIO().MousePos - min;
            拖拽有交换 = false;
        }

        // 拖拽换位/拖动面板进行中抑制悬停反馈, 避免高亮/提示跳动
        if (拖拽源 >= 0 || 按压已转拖动)
            hovered = false;

        // 状态描边：启用青 / 关闭血红; 笔触边框按状态色染色, 悬停微提亮
        var border = on ? SimplePalette.QtOnBorder : SimplePalette.QtOffBorder;
        if (hovered)
            border = new Vector4(Math.Min(1f, border.X + 0.15f), Math.Min(1f, border.Y + 0.15f),
                Math.Min(1f, border.Z + 0.15f), border.W);
        ShuimoDraw.DrawBrushStateFrame(drawList, min, max, border, on ? 7f : 6f);

        // 启用：底部约 3px 微光条（绿色）
        if (on)
            drawList.AddRectFilled(new Vector2(min.X + 7f, max.Y - 3f), new Vector2(max.X - 7f, max.Y - 1f),
                SimplePalette.ToU32(SimplePalette.QtOnGlow), 99f);

        if (hovered)
            ShuimoDraw.SetTooltipLight($"{def.Label}：{(on ? "已启用" : "已关闭")}\n左键开关{(locked ? "" : " · 右键拖动排序")}");
    }

    // QT 图标解析：使用方经 Nag0miUIJobEnv.Configure 注入的解析器;
    // 未注入或解析失败（null / iconId==0）→ 问号图标 + 显示名首字 marker。
    private static (uint iconId, bool isGameIcon, string? marker) 解析图标(QtDefinition def)
    {
        var r = Nag0miUIJobEnv.QtIconResolver?.Invoke(def.Id);
        if (r is { iconId: > 0 }) return r.Value;
        return (问号图标, true, string.IsNullOrEmpty(def.Label) ? null : def.Label[..1]);
    }

    // ============================================================
    // === 排列顺序与拖拽换位 ===
    // ============================================================
    // 把宿主解析出的 QT 清单按 Nag0miUISettings.QtOrder 的自定义顺序重排:
    // 顺序表里没有的键按原相对顺序置尾（LINQ OrderBy 稳定排序）。
    private static List<QtDefinition> 按自定义顺序排序(IReadOnlyList<QtDefinition> defs)
    {
        if (defs.Count < 2) return defs.ToList();
        var order = Nag0miUISettings.Instance.GetOrderedQtKeys();
        var rank = new Dictionary<string, int>(order.Count);
        for (var i = 0; i < order.Count; i++) rank[order[i]] = i;
        return defs.OrderBy(d => rank.TryGetValue(d.Id, out var r) ? r : int.MaxValue).ToList();
    }

    // 松手提交：把拖拽后的可见序列合并回完整顺序（隐藏键保持原槽位）, 写回设置并落盘。
    private static void 提交顺序(List<string> 新可见序列)
    {
        var s = Nag0miUISettings.Instance;
        var full = s.GetOrderedQtKeys();
        var 可见 = new HashSet<string>(新可见序列, StringComparer.Ordinal);
        var merged = new List<string>(full.Count);
        var vi = 0;
        foreach (var k in full)
            merged.Add(可见.Contains(k) ? 新可见序列[vi++] : k);
        s.QtOrder = merged;
        s.Save();
        APIHelper.重建QT可见性();
    }

    // 显示槽位 slot 相对网格原点的偏移（与 计算窗口尺寸 同一网格算法）。
    private static Vector2 格位偏移(int slot)
        => new Vector2(slot % 每行, slot / 每行) * new Vector2(格 + 间距, 格 + 间距);

    // 格子视觉位置指数阻尼收敛到目标格位（窗口相对坐标）; 新出现的格子直接落位, 不做飞入动画。
    private Vector2 取动画格位(string id, Vector2 目标)
    {
        if (!动画格位.TryGetValue(id, out var 当前))
        {
            动画格位[id] = 目标;
            return 目标;
        }

        var next = SwapOrderHelper.AnimatePosition(当前, 目标, ImGui.GetIO().DeltaTime);
        动画格位[id] = next;
        return next;
    }

    // QT 清单变化后移除失效条目的动画位置记录。
    private void 清理动画格位(IReadOnlyList<QtDefinition> defs)
    {
        if (动画格位.Count <= defs.Count) return;
        var live = new HashSet<string>(StringComparer.Ordinal);
        foreach (var def in defs) live.Add(def.Id);
        foreach (var id in 动画格位.Keys.Where(id => !live.Contains(id)).ToArray())
            动画格位.Remove(id);
    }

    // ============================================================
    // === 外壳与拖动 ===
    // ============================================================
    // 窗口尺寸 = 瓦片网格 + 内边距，精确贴合、无多余边。
    private static Vector2 计算窗口尺寸(int count)
    {
        if (count == 0) return new Vector2(260f, 110f);
        var cols = Math.Min(每行, count);
        var rows = (count + 每行 - 1) / 每行;
        var pad = ImGui.GetStyle().WindowPadding;
        return new Vector2(cols * 格 + (cols - 1) * 间距 + pad.X * 2f,
                           rows * 格 + (rows - 1) * 间距 + pad.Y * 2f);
    }

    // 面板底色与边框：画进窗口自身的绘制列表（先垫占位命令，见 Nag0miUILayer），
    // 保证两个窗口重叠时背景仍然盖住身后窗口的内容。
    // 水墨底 = 宣纸平铺（不透明度见个性化页窗口背景设置）+ 笔触边框；QT 面板不铺山水（瓦片太小只剩噪点）。
    private void DrawWindowChrome()
    {
        var pos = ImGui.GetWindowPos();
        var max = pos + ImGui.GetWindowSize();
        var drawList = ImGui.GetWindowDrawList();
        Nag0miUILayer.垫牺牲帧(drawList);
        // 覆盖 Begin 压入的内容区内层裁剪, 底色/边框画满全窗口（本窗无标题栏）
        drawList.PushClipRect(pos, max, false);

        // 自定义背景图（设置页「个性化」配置）优先于默认宣纸底; 纸底不透明度逐窗可调
        if (!WindowBackgroundManager.DrawBackgroundImage(drawList,
                Nag0miUICommonSettings.Instance.Qt面板背景, pos, ImGui.GetWindowSize(), 4f))
            ShuimoDraw.DrawPaper(drawList, pos, max, Nag0miUICommonSettings.Instance.Qt面板背景.BackgroundOpacity);
        drawList.PopClipRect();

        drawList.PushClipRectFullScreen();
        ShuimoDraw.DrawBrushFrame(drawList, pos, max);
        drawList.PopClipRect();
    }

    // 左键按住窗口内任意位置（瓦片或缝隙）拖动整个面板：瓦片上的点击与拖动按位移阈值区分，
    // 超过阈值即转拖动、松手不再触发 QT 开关；未超阈值松手仍是点击开关。位置钳制在屏幕工作区
    // 内，松手落盘。本方法在瓦片绘制之后调用：松手当帧先由 画瓦片 按 按压已转拖动 抑制点击，
    // 再到这里重置按压标志，时序不会错。
    private void HandleWindowDrag()
    {
        // 位置锁定（设置页勾选）: 不起拖也不残留按压状态, 点击开关不受影响
        if (Nag0miUISettings.Instance.Qt面板位置锁定)
        {
            左键按压在本窗 = false;
            按压已转拖动 = false;
            return;
        }

        // AllowWhenBlockedByActiveItem：按住瓦片（按钮占住 ActiveId）时悬停判定仍为真
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
                Nag0miUISettings.Instance.Qt面板位置 = ImGui.GetWindowPos();
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
