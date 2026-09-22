// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using Nag0mi.Common.Data;
using PromeRotation.Data;
using PromeRotation.UI.HotKey;

namespace Nag0mi.Common.Helper;

// 热键面板构建器：按注册顺序产出条目列表，交给悬浮面板自绘（不使用宿主默认皮肤）。
// 已在设置里隐藏的按钮名会自动过滤。
public sealed class Nag0miUIHotkeyBuilder
{
    private readonly HashSet<string> hidden;

    /// <summary>构建产物，条目顺序即面板排布顺序。GameIcon 为游戏内原始图标 id（物品等无动作表的条目用），GameIconHQ 表示取 HQ 品质图标。</summary>
    public List<(string Name, IHotkey Hotkey, uint GameIcon, bool GameIconHQ)> Entries { get; } = new();

    // 全量清单（不受显隐过滤, 与 Entries 共享同一 IHotkey 实例）：设置页「热键显隐」列表据此刻画图标。
    internal List<(string Name, IHotkey Hotkey, uint GameIcon, bool GameIconHQ)> AllEntries { get; } = new();

    // 供宿主 HotkeyManager 同步注册的全量清单（不受显隐过滤，时间轴热键节点据此查询/触发）。
    internal List<(string Name, PAction? Action, IHotkeyLogic? Logic, uint IconActionId, string? CustomIconPath)> HostEntries { get; } = new();

    public Nag0miUIHotkeyBuilder(Nag0miUISettings settings)
    {
        hidden = new HashSet<string>(settings.HiddenHotkeys);
    }

    /// <summary>注册一个固定技能按钮，点击时把该技能原样放进热键队列，图标自动取游戏内技能图标。</summary>
    public void Fixed(string name, uint spell, ActionType type, ActionTargetType target)
    {
        var action = new PAction(spell, type, target);
        HostEntries.Add((name, action, null, 0, null));
        var hk = new ActionHotkey(action);
        AllEntries.Add((name, hk, 0, false));
        if (!hidden.Contains(name))
            Entries.Add((name, hk, 0, false));
    }

    /// <summary>注册一个自定义逻辑按钮，点击时执行传入的 IHotkeyLogic。</summary>
    /// <param name="iconActionID">游戏内动作 id，按钮的图标、冷却、充能显示都来自它。</param>
    /// <param name="customIconPath">自定义图标路径（宿主 Resources 资源名或绝对路径 tex/png）。</param>
    /// <param name="gameIconID">游戏内原始图标 id，用于物品等不在动作表里的条目，优先于 iconActionID。</param>
    /// <param name="gameIconHQ">图标是否取 HQ 品质（游戏内 hq/ 子目录）。</param>
    public void Execute(string name, IHotkeyLogic logic, uint iconActionID = 0, string? customIconPath = null, uint gameIconID = 0, bool gameIconHQ = false)
    {
        HostEntries.Add((name, null, logic, iconActionID, customIconPath));
        var hk = new DelegateHotkey(logic, iconActionID, customIconPath);
        AllEntries.Add((name, hk, gameIconID, gameIconHQ));
        if (!hidden.Contains(name))
            Entries.Add((name, hk, gameIconID, gameIconHQ));
    }
}
