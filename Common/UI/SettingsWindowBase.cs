// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Nag0mi.Common.Data;

namespace Nag0mi.Common.UI;

// Nag0miUI 设置窗口基类（Dalamud WindowSystem 托管）。
// 移植时剔除主题系统：固定暗色 chrome（原夜间模式配色），页签列表完全由子类给出，
// 不追加「主题」页。侧边栏布局见 SettingsWindowBase.Sidebar.cs，配色常量见 SimplePalette。
public abstract partial class SettingsWindowBase : Window
{
    private int currentTab;
    private bool positionDirty;
    private bool wasOpen;

    private static readonly Vector2 DefaultSize = new(760f, 560f);
    private static readonly Vector2 DefaultPosition = new(100f, 100f);
    private const float MinWindowSize = 200f;

    // 基础全局样式的颜色 Push 数量（Pop 时使用；侧边栏 chrome 在此基础上增量 Push，见 SidebarChromeColorCount）。
    private const int BaseStyleColorCount = 22;

    // Tab 列表
    protected abstract string[] Tabs { get; }

    // 侧边栏分组：返回该页签所属分组名。非 null 时该页签作为分组子项缩进显示,
    // 分组标题画在组内首个页签之前、点击跳转该子栏；相邻页签返回同名组即归入同一组。默认无分组。
    protected virtual string? TabGroup(int tabIndex) => null;

    private string[]? tabsSource;

    // 当前页标签列表。子类 Tabs 引用变化时收敛 currentTab，避免索引越界。
    private string[] AllTabs
    {
        get
        {
            var tabs = Tabs;
            if (!ReferenceEquals(tabs, tabsSource))
            {
                // 按标签名保持当前页（动态页插入/移除导致索引位移时不跳页）
                var currentLabel = tabsSource != null && currentTab < tabsSource.Length ? tabsSource[currentTab] : null;
                tabsSource = tabs;
                var kept = currentLabel != null ? Array.IndexOf(tabs, currentLabel) : -1;
                currentTab = kept >= 0 ? kept : Math.Min(currentTab, tabs.Length - 1);
            }
            return tabs;
        }
    }

    // 绘制当前 Tab 的内容
    protected abstract void DrawTabContent(int tabIndex);

    // 窗口位置/尺寸持久化（可选，返回 null 则每帧实时读取）
    protected virtual WindowLayoutState? Layout => null;

    protected SettingsWindowBase(string title)
        : base(title, ImGuiWindowFlags.NoCollapse)
    {
        // 原生标题文字置空（无标题栏；### 后缀保持 ImGui 窗口标识不变）
        WindowName = $"###Nag0miUI.Settings.{GetType().Name}";
        // 最小尺寸兜底：防止历史损坏的持久化尺寸或 ImGui 异常把窗口锁死
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360f, 240f),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        Size = DefaultSize;
        SizeCondition = ImGuiCond.FirstUseEver;
        Position = DefaultPosition;
        PositionCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = false;
        AllowBackgroundBlur = false;
    }

    public override void PreDraw()
    {
        // 无标题栏：顶部栏整圈让给纸底与笔触边框, 拖动走侧边栏空白区（见 SettingsWindowBase.Sidebar.cs）
        Flags |= ImGuiWindowFlags.NoTitleBar;

        var layout = Layout;
        if (layout is { RememberPosition: true, Loaded: false })
        {
            layout.Loaded = true;

            // 历史数据可能已被垃圾值污染，只接受合理范围内的恢复值
            if (layout.SavedPosition is { } pos && IsSanePosition(pos))
            {
                Position = pos;
                PositionCondition = ImGuiCond.Once;
            }

            if (layout.SavedSize is { } size && IsSaneSize(size))
            {
                Size = size;
                SizeCondition = ImGuiCond.Once;
            }
        }

        PushGlobalStyle();
        base.PreDraw();
    }

    public override void Draw()
        // 自定义字体（设置页「个性化」配置; 未启用/加载失败时用宿主默认字体）
        => WindowFontManager.DrawWithCustomFont(Nag0miUICommonSettings.Instance.设置窗口字体, "设置窗口", DrawContentSafe);

    private void DrawContentSafe()
    {
        try
        {
            // 自定义背景图优先, 未启用/加载失败时画宣纸底
            DrawWindowBackground();

            DrawSidebarLayout();

            // 仅在窗口当前有效时捕获位置/尺寸；待用户拖动/缩放结束后一次性写盘
            CaptureLayout();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), $"绘制错误: {ex.Message}");
        }
    }

    // 内容区（滚动, 填满内容高度）。
    private void DrawContentChild()
    {
        ImGui.BeginChild("##content", new Vector2(0f, 0f), false);
        try
        {
            DrawTabContent(currentTab);
        }
        catch (Exception ex)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), $"绘制错误: {ex.Message}");
        }
        ImGui.EndChild();
    }

    public override void PostDraw()
    {
        // 窗口刚被关闭：把最近一次捕获的脏数据落盘
        if (wasOpen && !IsOpen)
            FlushLayout();

        wasOpen = IsOpen;
        PopGlobalStyle();
        base.PostDraw();
    }

    private void CaptureLayout()
    {
        var layout = Layout;
        if (layout is not { RememberPosition: true }) return;

        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        if (!IsSanePosition(pos) || !IsSaneSize(size)) return;

        layout.SavedPosition = pos;
        layout.SavedSize = size;

        // 用户已松手（拖动/缩放结束）才落盘一次，避免拖动过程中频繁写 JSON
        if (positionDirty && !ImGui.IsAnyItemActive() && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            positionDirty = false;
            layout.Save?.Invoke();
        }
        else if (ImGui.IsAnyItemActive() || ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            positionDirty = true;
        }
    }

    private void FlushLayout()
    {
        if (!positionDirty) return;
        positionDirty = false;
        Layout?.Save?.Invoke();
    }

    private static bool IsSaneSize(Vector2 v) =>
        v.X >= MinWindowSize && v.Y >= MinWindowSize && v.X < 10000f && v.Y < 10000f;

    private static bool IsSanePosition(Vector2 v) =>
        v.X >= -2000f && v.Y >= -2000f && v.X < 10000f && v.Y < 10000f;

    private void DrawWindowBackground()
    {
        // 仅主视口生效
        if (ImGui.GetWindowViewport().ID != ImGui.GetMainViewport().ID) return;

        // 底色画进窗口自身的绘制列表，先垫占位命令防止背景模糊顶掉首条绘制（见 Nag0miUILayer），
        // 保证两个窗口重叠时背景仍然盖住身后窗口的内容
        var min = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();

        var drawList = ImGui.GetWindowDrawList();
        Nag0miUILayer.垫牺牲帧(drawList);
        // 背景画满全窗口（无标题栏）: Begin 给窗口 draw list 压的是内容区内层裁剪（减内边距）,
        // 不覆盖会把底色四条边各裁掉一条
        drawList.PushClipRect(min, min + size, false);
        // 自定义背景图（设置页「个性化」配置）优先于默认宣纸底；
        // 默认底 = 宣纸平铺 + 山水装饰层（纸底之上、内容之下, 非交互）
        if (!WindowBackgroundManager.DrawBackgroundImage(drawList,
                Nag0miUICommonSettings.Instance.设置窗口背景, min + new Vector2(0.5f), size - new Vector2(1f), 4f))
        {
            ShuimoDraw.DrawPaper(drawList, min, min + size, 0.95f);
            ShuimoDraw.DrawMountains(drawList, min, min + size);
        }
        drawList.PopClipRect();

        // 窗口外壳笔触边框（9-slice, 整圈）
        drawList.PushClipRectFullScreen();
        ShuimoDraw.DrawBrushFrame(drawList, min, min + size);
        drawList.PopClipRect();
    }

    private void PushGlobalStyle()
    {
        // 窗口 + 内容区 child 都透明，背景统一由 DrawWindowBackground 自绘
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.Border, SimplePalette.Border);

        // 文本
        ImGui.PushStyleColor(ImGuiCol.Text, SimplePalette.TextPrimary);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, SimplePalette.TextDisabled);

        // 按钮（Tab 用，其它由主题色点缀）
        ImGui.PushStyleColor(ImGuiCol.Button, SimplePalette.FrameBg);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SimplePalette.FrameBgHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, SimplePalette.FrameBgActive);

        // FrameBg（Checkbox/Radio/Input/Drag 背景）
        ImGui.PushStyleColor(ImGuiCol.FrameBg, SimplePalette.FrameBg);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, SimplePalette.FrameBgHovered);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, SimplePalette.FrameBgActive);

        // 强调色：Checkbox 勾选、Drag grab、Slider grab
        ImGui.PushStyleColor(ImGuiCol.CheckMark, SimplePalette.Accent);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, SimplePalette.Accent);
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, SimplePalette.PrimaryActive);

        // Header（Combo 下拉项）
        ImGui.PushStyleColor(ImGuiCol.Header, SimplePalette.FrameBgHovered);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, SimplePalette.FrameBgActive);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, SimplePalette.FrameBgActive);

        // Popup（Combo 下拉等）
        ImGui.PushStyleColor(ImGuiCol.PopupBg, SimplePalette.PopupBg);

        // Separator
        ImGui.PushStyleColor(ImGuiCol.Separator, SimplePalette.Border);

        // Resize grip
        ImGui.PushStyleColor(ImGuiCol.ResizeGrip, SimplePalette.WithAlpha(SimplePalette.Primary, 0.2f));
        ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, SimplePalette.WithAlpha(SimplePalette.Primary, 0.4f));
        ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, SimplePalette.Primary);

        PushChromeColors();

        // 变量（窗口圆角 4：纸页的直角微圆感; 原生 1px 边框关闭, 改由笔触边框承载;
        // 内边距 16 ≥ 外壳笔触边框 12, 给笔触留完整展开空间）
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3f);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 3f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16f, 16f));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 4f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(6f, 4f));
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 10f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
    }

    // 暗色 chrome 样式：滚动条跟随主色调（原 ErosUI 夜间模式实现，内联为唯一一套）。
    // Push 数量必须等于 SidebarChromeColorCount，改动后同步该常量。
    private void PushChromeColors()
    {
        var primary = SimplePalette.Primary;
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0f, 0f, 0f, 0.20f));
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, SimplePalette.WithAlpha(primary, 0.45f));
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, SimplePalette.WithAlpha(primary, 0.65f));
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, SimplePalette.WithAlpha(primary, 0.85f));
    }

    private void PopGlobalStyle()
    {
        ImGui.PopStyleVar(9);
        ImGui.PopStyleColor(BaseStyleColorCount + SidebarChromeColorCount);
    }

    // 窗口位置/尺寸持久化状态，由子类持有。
    public sealed class WindowLayoutState
    {
        public bool Loaded;
        public bool RememberPosition = true;
        public Vector2? SavedPosition;
        public Vector2? SavedSize;

        // 保存回调；子类在构造完成后赋值。
        public System.Action? Save;
    }
}
