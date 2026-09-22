// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using Nag0mi.Common.Data;
using PromeRotation.Data;

namespace Nag0mi.Common;

// 框架的 QT 数据入口，只包含两个带附加逻辑的能力：
// 联动写入（在宿主 SetQt 基础上叠加联动表）和可见性重建（按职业与模式重新注册 QT）。
// 读 QT、屏幕提示、日志等与宿主一一等价的能力不在这里转发，直接用宿主 SDK。
internal static class APIHelper
{
    #region QT 读写

    /// <summary>写入 QT 开关状态，并按联动表把关联的键一并写入。</summary>
    internal static void 设置QT(string qtKey, bool 值)
    {
        PromeSettings.Instance.SetQt(qtKey, 值);
        var dict = PromeSettings.Instance.QuickToggles;
        if (Nag0miUIJobEnv.QtCascadeRules.TryGetValue(qtKey, out var links))
            foreach ((string lk, bool inv) in links)
                dict[lk] = inv ? !值 : 值;
    }

    /// <summary>清空后按当前职业与当前模式重新注册 QT，并同步显隐配置到宿主。</summary>
    /// <remarks>宿主的 QuickToggles 是全局表，重建时会顺带清掉其它职业注册的键，
    /// 多个 ACR 共存时互不残留。切换职业或模式后调用。</remarks>
    internal static void 重建QT可见性()
    {
        var modeIndex = Nag0miUISettings.Instance.ModeIndex;
        var qt = PromeSettings.Instance.QuickToggles;
        var saved = new Dictionary<string, bool>();
        foreach (var (key, _) in Nag0miUIJobEnv.QtAll)
            if (qt.TryGetValue(key, out var v)) saved[key] = v;
        PromeSettings.Instance.ClearQts();
        foreach (var k in qt.Keys.ToList())
            if (!Nag0miUIJobEnv.QtAll.ContainsKey(k)) qt.Remove(k);
        // 按用户自定义顺序注册（悬浮面板右键拖拽调整; 未调整过 = QT 表定义顺序）,
        // 宿主 QuickToggles 键序与悬浮面板保持同序
        foreach (var key in Nag0miUISettings.Instance.GetOrderedQtKeys())
        {
            if (!Nag0miUIJobEnv.QtIsVisibleInMode(key, modeIndex)) continue;
            var val = saved.TryGetValue(key, out var sv) ? sv : Nag0miUIJobEnv.QtDefault(key);
            PromeSettings.Instance.AddQt(key, val);
            qt[key] = val;
        }
        // PR 的 ClearQts 会连 HiddenQts 一起清空（宿主实测），重建后必须立刻按显隐配置回填，
        // 否则 QT管理 里隐藏的按钮在每次模式切换/进本后全部复活
        Nag0miUISettings.Instance.SyncQtHiddenToPr();
    }

    #endregion
}
