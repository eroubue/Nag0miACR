// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using Nag0mi.Common.Data;
using Nag0mi.Common.Helper;
using PromeRotation.UI.HotKey;

namespace Nag0mi.Common.UI;

// 热键面板管理：负责悬浮窗的构建、摘除与显隐。
// 面板条目完全由使用方经 Nag0miUIJobEnv.BuildHotkeys 注入，框架只负责
// 窗口外壳、布局参数、显隐与拖拽排序。
internal static class Nag0miUIHotkeyUI
{
    private static Nag0miUIHotkeyPanelWindow? window;

    // 注册进宿主 HotkeyManager 的影子面板：永不显示，只为时间轴"热键"节点提供
    // GetHotkeyNames/TryExecuteHotkey 的查询与触发能力（含被显隐设置隐藏的条目）。
    private static HotkeyPanel? hostShadow;

    /// <summary>（重新）构建热键面板。布局/显隐改动后调用；全部条目都被隐藏时不注册空面板。</summary>
    internal static void Rebuild()
    {
        Uninstall();

        var b = new Nag0miUIHotkeyBuilder(Nag0miUISettings.Instance);
        Nag0miUIJobEnv.BuildHotkeys?.Invoke(b);

        // 自定义热键（设置页 Hotkey 页管理）：走 Execute 通道, 图标/冷却/待发高亮由面板现有逻辑覆盖,
        // 同时进影子面板, 时间轴热键节点可按名触发
        foreach (var e in Nag0miUISettings.Instance.CustomHotkeys)
            b.Execute(e.Name, new ExecuteLogic(() => CustomHotkeyExecutor.Fire(e)), iconActionID: e.SkillId);

        RegisterHostShadow(b);

        if (b.Entries.Count == 0) return;

        var s = Nag0miUISettings.Instance;
        window = new Nag0miUIHotkeyPanelWindow(
            b.Entries,
            s.HotkeyColumns,
            45f * s.HotkeyScalePercent / 100f,
            s.HotkeySpacing);
        try { PromeRotation.Plugin.Instance?.WindowSystem.AddWindow(window); }
        catch { /* 宿主未就绪 */ }
    }

    /// <summary>从宿主 WindowSystem 摘除热键面板并注销影子面板。OnExitAcr 调用。</summary>
    internal static void Uninstall()
    {
        if (window != null)
        {
            try { PromeRotation.Plugin.Instance?.WindowSystem.RemoveWindow(window); }
            catch (ArgumentException) { /* 窗口可能已被宿主移除 */ }
            window = null;
        }

        if (hostShadow != null)
        {
            try { HotkeyManager.Instance.RemoveHotkeyPanel(hostShadow); }
            catch { /* 宿主可能已自行清理 */ }
            hostShadow = null;
        }
    }

    private static void RegisterHostShadow(Nag0miUIHotkeyBuilder b)
    {
        if (b.HostEntries.Count == 0) return;
        try
        {
            var panel = new HotkeyPanel(1, 1f, 0f, $"{Nag0miUIJobEnv.作者}·热键影子");
            foreach (var (name, action, logic, iconActionId, customIconPath) in b.HostEntries)
            {
                if (action != null) panel.AddHotkey(name, action);
                else if (logic != null) panel.AddHotkey(name, logic, iconActionId, customIconPath);
            }
            HotkeyManager.Instance.AddHotkeyPanel(panel);
            panel.IsOpen = false;
            hostShadow = panel;
        }
        catch { /* 宿主未就绪 */ }
    }

    /// <summary>热键面板当前是否可见，未构建时为 false。</summary>
    internal static bool PanelVisible => window?.IsOpen ?? false;

    /// <summary>显示/隐藏热键面板。只切换显隐，不重建窗口。</summary>
    internal static void SetPanelVisible(bool visible)
    {
        if (window == null) return;
        window.IsOpen = visible;
    }
}
