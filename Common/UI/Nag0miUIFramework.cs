// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Nag0mi.Common.Data;
using Nag0mi.Common.Helper;
using Nag0mi.Gunbreaker.Control;
using PromeRotation;

namespace Nag0mi.Common.UI;

// Nag0miUI 框架门面：使用方的唯一入口。Configure 注入职业环境，
// Install/Uninstall 一站式装卸（WindowLease 接管宿主设置窗、注册三窗口、热键面板、
// QT 重建、设置落盘全部内含），必须严格成对地放在 OnEnterAcr/OnExitAcr 里。
public static class Nag0miUIFramework
{
    private static Nag0miUIControlWindow? control;
    private static Nag0miUISettingsWindow? settings;
    private static Nag0miUIQtPanelWindow? qtPanel;
    private static WindowLease<Window>? windows;

    /// <summary>框架当前是否已安装（供 RotationEventHandler 等无引用访问点判断）。</summary>
    public static bool Active { get; private set; }

    /// <summary>注入本职业的全部环境。在 Rotation 构造函数最前面调用。</summary>
    public static void Configure(
        string jobTag,
        string jobName,
        IReadOnlyDictionary<string, bool> qtAll,
        Func<string, bool> qtIsMetaKey,
        Func<string, int, bool> qtIsVisibleInMode,
        Func<string, bool> qtDefault,
        IReadOnlyDictionary<string, (string key, bool invert)[]> qtCascadeRules,
        string[] hotkeyNames,
        System.Action<Nag0miUIHotkeyBuilder>? buildHotkeys,
        (string key, string label, uint skill)[]? qtTab基础 = null,
        (string key, string label, uint skill)[]? qtTab技能 = null,
        (string key, string label, uint skill)[]? qtTab资源 = null,
        int modeCount = 2,
        string[]? modeNames = null,
        string? author = null,
        Func<string, (uint iconId, bool isGameIcon, string? marker)>? qtIconResolver = null,
        Func<string, int, bool>? qtDefaultVisible = null,
        (string label, System.Action draw)[]? extraTabs = null,
        System.Action? cycleMode = null,
        Func<string>? currentModeLabel = null,
        (uint Id, string Name, PromeRotation.Data.ActionType Type)[]? customHotkeySkills = null,
        IReadOnlyDictionary<uint, (uint BuffId, bool SelfOnly)>? hotkeyActiveBuffs = null)
        => Nag0miUIJobEnv.Configure(jobTag, jobName, qtAll, qtIsMetaKey, qtIsVisibleInMode,
            qtDefault, qtCascadeRules, hotkeyNames, buildHotkeys, qtTab基础, qtTab技能, qtTab资源,
            modeCount, modeNames, author, qtIconResolver, qtDefaultVisible, extraTabs, cycleMode, currentModeLabel,
            customHotkeySkills, hotkeyActiveBuffs);

    /// <summary>注册全部窗口并完成初始化：接管宿主设置窗、加入控制条/设置窗/QT面板、
    /// 构建热键面板、重建 QT、压制宿主自带面板。幂等，重复调用会先卸载再注册。OnEnterAcr 调用。</summary>
    public static void Install()
    {
        Uninstall();
        var host = Plugin.Instance;
        var ws = host?.WindowSystem;
        if (host == null || ws == null)
        {
            ECommons.Logging.PluginLog.Warning($"[{Nag0miUIJobEnv.作者}] 无法访问宿主 WindowSystem，UI 未注册");
            return;
        }

        control = new Nag0miUIControlWindow();
        settings = new Nag0miUISettingsWindow();
        qtPanel = new Nag0miUIQtPanelWindow();

        // WindowLease 接管宿主：把宿主 SettingsWindow 移出 WindowSystem 并记住其注册/显隐状态，
        // 加入自家三窗口；Dispose 时逆序恢复。
        windows = new WindowLease<Window>(host.SettingsWindow,
            new Window[] { control, qtPanel, settings },
            window => ws.Windows.Contains(window),
            ws.AddWindow, ws.RemoveWindow,
            window => window.IsOpen, (window, open) => window.IsOpen = open);
        windows.Activate();
        qtPanel.IsOpen = true;   // 默认打开

        Nag0miUIHotkeyUI.Rebuild();
        APIHelper.重建QT可见性();
        HidePrPanels(force: true);   // 切职业瞬间宿主会重弹自带面板，强制压制一次
        Active = true;
    }

    /// <summary>卸载全部窗口与热键面板，恢复宿主设置窗，并把两份设置落盘。OnExitAcr 调用，与 Install 严格成对。</summary>
    public static void Uninstall()
    {
        Active = false;
        Nag0miUIHotkeyUI.Uninstall();
        if (windows != null)
        {
            try { windows.Dispose(); } catch { /* 宿主可能已改动窗口注册 */ }
            windows = null;
        }
        control = null;
        settings = null;
        qtPanel = null;
        SaveSettings();
    }

    /// <summary>把通用设置与当前职业设置立即写入磁盘。</summary>
    public static void SaveSettings()
    {
        Nag0miUISettings.Instance.Save();
        Nag0miUICommonSettings.Instance.Save();
    }

    /// <summary>写入 QT 开关状态，并按联动表把关联的键一并写入。</summary>
    public static void 设置QT(string qtKey, bool 值) => APIHelper.设置QT(qtKey, 值);

    /// <summary>清空后按当前职业与当前模式重新注册 QT，并同步显隐配置到宿主。切换模式后调用。</summary>
    public static void 重建QT可见性() => APIHelper.重建QT可见性();

    /// <summary>切换 QT 悬浮面板的显示/隐藏。</summary>
    public static void ToggleQtPanel()
    {
        if (qtPanel == null) return;
        qtPanel.IsOpen = !qtPanel.IsOpen;
    }

    /// <summary>QT 面板与热键面板是否至少有一个在显示（设置页「面板控制」按钮的状态依据）。</summary>
    public static bool 任一面板可见 => (qtPanel?.IsOpen ?? false) || Nag0miUIHotkeyUI.PanelVisible;

    /// <summary>一键显示/隐藏 QT 面板与热键面板。只切换显隐，不重建窗口。</summary>
    public static void SetPanelsVisible(bool visible)
    {
        if (qtPanel != null) qtPanel.IsOpen = visible;
        Nag0miUIHotkeyUI.SetPanelVisible(visible);
    }

    /// <summary>打开设置窗口。</summary>
    public static void OpenSettings()
    {
        if (settings != null) settings.IsOpen = true;
    }

    /// <summary>切换设置窗口的打开/关闭；打开时贴着控制条展开（控制条「设置」键使用）。</summary>
    public static void ToggleSettings()
    {
        if (settings == null) return;
        if (settings.IsOpen)
        {
            settings.IsOpen = false;
            return;
        }
        if (control != null)
            settings.OpenNearBar(control.PixelPosition, control.PixelSize, control.Placement.Side);
        else
            settings.IsOpen = true;
    }

    /// <summary>给宿主 DrawSettings 回调用的入口：绘制一个打开设置窗口的按钮。</summary>
    public static void DrawSettingsEntry()
    {
        if (ImGui.Button($"打开 {Nag0miUIJobEnv.作者} 设置窗口"))
            OpenSettings();
    }

    // ============================================================
    // === 宿主本体面板压制 ===
    // ============================================================
    private static long _上次HidePrPanels;

    /// <summary>关闭宿主自带的 QT 面板窗口，防止它与本框架的面板同时出现。</summary>
    /// <remarks>
    /// 只能在 UI 线程调用（由控制条每帧触发，内部 1 秒节流）。
    /// 不要挂到 Framework.Update：那会在渲染线程之外改窗口状态，与渲染竞争后
    /// 会导致整个游戏的 ImGui 交互假死。force 参数绕过节流立即执行，供切职业时
    /// 抑制宿主重新弹出的面板。
    /// </remarks>
    public static void HidePrPanels(bool force = false)
    {
        var now = Environment.TickCount64;
        if (!force && now - _上次HidePrPanels < 1000) return;
        _上次HidePrPanels = now;
        try { Plugin.Instance?.CloseQtWindow(); } catch { /* 宿主未就绪 */ }
    }
}
