using PromeRotation.Timeline.Core;
using PromeRotation.UI.HotKey;

namespace Nag0mi.Gunbreaker.Timeline;

/// <summary>
/// 绝枪/热键：时间轴触发已注册的绝枪热键（等价于点击对应热键按钮）。
/// </summary>
public sealed class GunbreakerHotkeyAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    internal const string TypeKey = "gnbhotkey";

    private const string HotkeyKey = "hotkey";

    private string _hotkey = "";

    public string NodeDisplayName => "绝枪/热键";

    public NodeParamInfo[] Params
    {
        get
        {
            var names = HotkeyManager.Instance.GetHotkeyNames().ToArray();
            var options = new (string Value, string Label)[names.Length];
            for (var i = 0; i < names.Length; i++)
                options[i] = (names[i], names[i]);

            return new NodeParamInfo[]
            {
                new(HotkeyKey, "热键", names.Length == 0 ? "当前没有已注册的绝枪热键" : "选择要触发的热键", "enum", options),
            };
        }
    }

    public string GetParam(string fieldName) => fieldName == HotkeyKey ? _hotkey : "";

    public void SetParam(string fieldName, string value)
    {
        if (fieldName == HotkeyKey)
            _hotkey = value;
    }

    public void Execute()
    {
        if (!string.IsNullOrWhiteSpace(_hotkey))
            HotkeyManager.Instance.TryExecuteHotkey(_hotkey);
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string> { [HotkeyKey] = _hotkey },
    };

    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, TypeKey, FromDto);
    }

    private static GunbreakerHotkeyAction FromDto(ActionDto dto)
    {
        var action = new GunbreakerHotkeyAction();
        if (dto.Params != null && dto.Params.TryGetValue(HotkeyKey, out var hotkey))
            action._hotkey = hotkey;

        return action;
    }
}
