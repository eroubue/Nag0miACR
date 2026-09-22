// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Nag0mi.Common.Data;
using Nag0mi.Gunbreaker.Control;

namespace Nag0mi.Common.UI;

// Nag0miUI 设置窗口（SettingsWindowBase 子类，多职业共用同一窗口类）。
// 固定页签：面板控制 / 基础设置（两者归入侧边栏「基础设置」分组子栏）/ Hotkey / QT面板 / 个性化；
// 使用方经 Nag0miUIJobEnv.Configure 注入的 extraTabs 追加在固定页之后
// （原 ErosUI 的末位「主题」页已随主题系统一并剔除）。
public sealed class Nag0miUISettingsWindow : SettingsWindowBase
{
    // 通用设置里的布局记录键（WindowLayouts 字典）
    private const string LayoutKey = "Settings";

    private readonly WindowLayoutState layout = new();

    // extraTabs 变更时重建页签数组（基类按引用比较收敛当前页）
    private string[]? tabsCache;
    private (string label, System.Action draw)[]? tabsCacheSource;

    public Nag0miUISettingsWindow() : base($"{Nag0miUIJobEnv.作者} {Nag0miUIJobEnv.JobName}设置")
    {
        // 三项 Allow 全关 → Dalamud 不再往标题栏注入「窗口设置」齿轮，
        // 右上角只剩关闭键（ImGui 自绘，不受影响）
        AllowPinning = false;
        AllowClickthrough = false;
        AllowBackgroundBlur = false;

        // 布局持久化：种子从通用设置读，保存写回并落盘
        var data = LayoutData;
        layout.SavedPosition = data.Position;
        layout.SavedSize = data.Size;
        layout.Save = () =>
        {
            var d = LayoutData;
            d.Position = layout.SavedPosition;
            d.Size = layout.SavedSize;
            Nag0miUICommonSettings.Instance.Save();
        };
    }

    private static Nag0miUICommonSettings.WindowLayoutData LayoutData
    {
        get
        {
            var layouts = Nag0miUICommonSettings.Instance.WindowLayouts;
            if (!layouts.TryGetValue(LayoutKey, out var d))
                layouts[LayoutKey] = d = new Nag0miUICommonSettings.WindowLayoutData();
            return d;
        }
    }

    protected override string[] Tabs
    {
        get
        {
            var extra = Nag0miUIJobEnv.ExtraTabs;
            if (tabsCache == null || !ReferenceEquals(extra, tabsCacheSource))
            {
                tabsCacheSource = extra;
                tabsCache = new string[5 + extra.Length];
                tabsCache[0] = "面板控制";
                tabsCache[1] = "基础设置";
                tabsCache[2] = "Hotkey";
                tabsCache[3] = "QT面板";
                tabsCache[4] = "个性化";
                for (var i = 0; i < extra.Length; i++)
                    tabsCache[5 + i] = extra[i].label;
            }
            return tabsCache;
        }
    }

    // 「面板控制」「基础设置」两栏归入侧边栏「基础设置」分组，作为其子栏缩进显示
    protected override string? TabGroup(int tabIndex) => tabIndex is 0 or 1 ? "基础设置" : null;

    protected override WindowLayoutState? Layout => layout;

    protected override void DrawTabContent(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0: Nag0miUISettingsUI.DrawPanelControl(); break;
            case 1: Nag0miUISettingsUI.DrawBasicSettings(); break;
            case 2: Nag0miUISettingsUI.DrawHotkey(); break;
            case 3: Nag0miUISettingsUI.DrawQtPanel(); break;
            case 4: Nag0miUISettingsUI.DrawPersonalization(); break;
            default:
                var extra = Nag0miUIJobEnv.ExtraTabs;
                var i = tabIndex - 5;
                if (i >= 0 && i < extra.Length) extra[i].draw();
                break;
        }
    }

    // 贴条展开：在控制条旁打开（控制条「设置」键使用），位置按吸附侧选左右。
    internal void OpenNearBar(Vector2 barPixelPosition, Vector2 barPixelSize, SnapSide side)
    {
        var vp = ImGui.GetMainViewport();
        var scale = ImGuiHelpers.GlobalScale;
        var size = Size ?? new Vector2(760f, 560f);
        // 本窗未开 ForceMainWindow，Position 是屏幕绝对坐标，直接取用;
        // Once: 只在本次打开时落位——Always 会每帧强制回写该位置, 窗口被钉死无法拖动
        Position = OverlayGeometry.SettingsPosition(barPixelPosition, barPixelSize, side,
            vp.Pos, vp.Size, size * scale, scale);
        PositionCondition = ImGuiCond.Once;
        IsOpen = true;
        BringToFront();
    }
}
