using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;

namespace Nag0mi.Gunbreaker.Timeline;

/// <summary>
/// 绝枪/检测量谱-子弹比较：按比较符判断当前子弹数量。
/// </summary>
public sealed class GunbreakerAmmoCondition : ICondition, IImmediateCondition, ISerializableCondition, IJobNodeDescriptor
{
    internal const string TypeKey = "gnbammo";

    private const string AmmoKey = "ammo";
    private const string OperationKey = "op";

    private enum CompareOperation
    {
        大于等于,
        大于,
        等于,
        小于,
        小于等于,
    }

    private static readonly (string Value, string Label)[] OperationOptions =
    {
        (nameof(CompareOperation.大于等于), ">="),
        (nameof(CompareOperation.大于), ">"),
        (nameof(CompareOperation.等于), "="),
        (nameof(CompareOperation.小于), "<"),
        (nameof(CompareOperation.小于等于), "<="),
    };

    private int _ammo;
    private string _operation = nameof(CompareOperation.大于等于);

    public string NodeDisplayName => "绝枪/检测量谱-子弹比较";

    public NodeParamInfo[] Params => new NodeParamInfo[]
    {
        new(AmmoKey, "子弹数量", "要比较的子弹数量", "int"),
        new(OperationKey, "比较操作", "当前子弹数量 [比较操作] 子弹数量", "enum", OperationOptions),
    };

    public string GetParam(string fieldName) => fieldName switch
    {
        AmmoKey => _ammo.ToString(),
        OperationKey => _operation,
        _ => "",
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case AmmoKey:
                if (int.TryParse(value, out var ammo)) _ammo = ammo;
                break;
            case OperationKey:
                if (Enum.TryParse<CompareOperation>(value, out var operation)) _operation = operation.ToString();
                break;
        }
    }

    public bool EvaluateImmediate() => Compare(JobGaugeHelper.GNB.Ammo);

    public bool EvaluateWait() => EvaluateImmediate();

    private bool Compare(int current)
    {
        if (!Enum.TryParse<CompareOperation>(_operation, out var operation))
            operation = CompareOperation.大于等于;

        return operation switch
        {
            CompareOperation.大于等于 => current >= _ammo,
            CompareOperation.大于 => current > _ammo,
            CompareOperation.等于 => current == _ammo,
            CompareOperation.小于 => current < _ammo,
            CompareOperation.小于等于 => current <= _ammo,
            _ => false,
        };
    }

    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string>
        {
            [AmmoKey] = _ammo.ToString(),
            [OperationKey] = _operation,
        },
    };

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, TypeKey, FromDto);
    }

    private static GunbreakerAmmoCondition FromDto(ConditionDto dto)
    {
        var condition = new GunbreakerAmmoCondition();
        if (dto.Params == null)
            return condition;

        if (dto.Params.TryGetValue(AmmoKey, out var ammo)) condition.SetParam(AmmoKey, ammo);
        if (dto.Params.TryGetValue(OperationKey, out var operation)) condition.SetParam(OperationKey, operation);

        return condition;
    }
}
