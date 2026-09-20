using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Timeline.Core;

namespace Nag0mi.Gunbreaker.Timeline;

/// <summary>
/// 绝枪/QT：时间轴批量设置绝枪 QT 开关。
/// 每个 QT 一个三态下拉（未添加 / 已关闭 / 已启用），未添加的 QT 不会被执行。
/// </summary>
public sealed class GunbreakerQtAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    internal const string TypeKey = "gnbqt";

    private static readonly (string Value, string Label)[] StateOptions =
    {
        ("", "未添加"),
        ("False", "已关闭"),
        ("True", "已启用"),
    };

    private readonly Dictionary<string, bool> _states = new();

    public string NodeDisplayName => "绝枪/QT";

    public NodeParamInfo[] Params
    {
        get
        {
            var result = new List<NodeParamInfo>();
            foreach (var qt in GunbreakerQtDefaults.All.Keys)
            {
                result.Add(new NodeParamInfo(qt, qt, "未添加 = 不修改；已启用 / 已关闭 = 执行时写入该状态",
                    "enum", StateOptions));
            }

            return result.ToArray();
        }
    }

    public string GetParam(string fieldName)
        => _states.TryGetValue(fieldName, out var value) ? value.ToString() : "";

    public void SetParam(string fieldName, string value)
    {
        if (string.IsNullOrEmpty(value))
            _states.Remove(fieldName);
        else if (bool.TryParse(value, out var enabled))
            _states[fieldName] = enabled;
    }

    public void Execute()
    {
        foreach (var (qt, enabled) in _states)
        {
            if (!PromeSettings.Instance.QuickToggles.ContainsKey(qt))
                continue;

            PromeSettings.Instance.SetQt(qt, enabled);
        }
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = _states.ToDictionary(pair => pair.Key, pair => pair.Value.ToString()),
    };

    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, TypeKey, dto => FromDto(dto));
    }

    private static GunbreakerQtAction FromDto(ActionDto dto)
    {
        var action = new GunbreakerQtAction();
        if (dto.Params == null)
            return action;

        foreach (var (key, value) in dto.Params)
        {
            if (bool.TryParse(value, out var enabled))
                action._states[key] = enabled;
        }

        return action;
    }
}
