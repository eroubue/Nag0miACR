using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;

namespace Nag0mi.Gunbreaker.Timeline;

/// <summary>
/// 绝枪/检测连击进度：判断上一个连击 GCD 处于第几段（1-3）。
/// 覆盖单体、子弹连、狮心连与 AOE 连击。
/// </summary>
public sealed class GunbreakerComboCondition : ICondition, IImmediateCondition, ISerializableCondition, IJobNodeDescriptor
{
    internal const string TypeKey = "gnbcombo";

    private const string ComboKey = "combo";

    private int _combo = 1;

    public string NodeDisplayName => "绝枪/检测连击进度";

    public NodeParamInfo[] Params => new NodeParamInfo[]
    {
        new(ComboKey, "连击进度(1-3)", "匹配上一个连击 GCD 的进度", "int"),
    };

    public string GetParam(string fieldName) => fieldName == ComboKey ? _combo.ToString() : "";

    public void SetParam(string fieldName, string value)
    {
        if (fieldName == ComboKey && int.TryParse(value, out var combo))
            _combo = combo;
    }

    public bool EvaluateImmediate() => GetComboStage(ActionHelper.GetLastComboID()) == _combo;

    public bool EvaluateWait() => EvaluateImmediate();

    private static int GetComboStage(uint spellId) => spellId switch
    {
        16137 => 1, // 利刃斩 - 单体1
        16146 => 1, // 烈牙 - 子弹1
        36937 => 1, // 崛起之心 - 狮心1
        16141 => 1, // 恶魔切 - AOE1
        16139 => 2, // 残暴弹 - 单体2
        16147 => 2, // 猛兽爪 - 子弹2
        36938 => 2, // 支配之心 - 狮心2
        16149 => 2, // 恶魔杀 - AOE2
        16145 => 3, // 迅连斩 - 单体3
        16150 => 3, // 凶禽爪 - 子弹3
        36939 => 3, // 终结之心 - 狮心3
        _ => 0,
    };

    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string> { [ComboKey] = _combo.ToString() },
    };

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, TypeKey, FromDto);
    }

    private static GunbreakerComboCondition FromDto(ConditionDto dto)
    {
        var condition = new GunbreakerComboCondition();
        if (dto.Params != null && dto.Params.TryGetValue(ComboKey, out var combo))
            condition.SetParam(ComboKey, combo);

        return condition;
    }
}
