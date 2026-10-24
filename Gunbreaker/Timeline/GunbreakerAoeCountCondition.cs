using Nag0mi.Gunbreaker.Data;
using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;

namespace Nag0mi.Gunbreaker.Timeline;

/// <summary>
/// 绝枪/检测5m有效AOE敌数：统计自身 5m 内敌人数，减去预计活不过当前 AOE 连击窗口的将死敌数，按比较符判断。
/// 将死判定与 AOE 循环一致：连击窗口 =（连击已在恶魔切后 ? 1 : 2）× 当前 GCD 时长。
/// </summary>
public sealed class GunbreakerAoeCountCondition : ICondition, IImmediateCondition, ISerializableCondition, IJobNodeDescriptor
{
    internal const string TypeKey = "gnbaoecount";

    private const string CountKey = "count";
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

    private int _count;
    private string _operation = nameof(CompareOperation.大于等于);

    public string NodeDisplayName => "绝枪/检测5m有效AOE敌数";

    public NodeParamInfo[] Params => new NodeParamInfo[]
    {
        new(CountKey, "敌数", "要比较的 5m 内有效敌数（实际数量 - 将死数量）", "int"),
        new(OperationKey, "比较操作", "当前有效敌数 [比较操作] 敌数", "enum", OperationOptions),
    };

    public string GetParam(string fieldName) => fieldName switch
    {
        CountKey => _count.ToString(),
        OperationKey => _operation,
        _ => "",
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case CountKey:
                if (int.TryParse(value, out var count)) _count = count;
                break;
            case OperationKey:
                if (Enum.TryParse<CompareOperation>(value, out var operation)) _operation = operation.ToString();
                break;
        }
    }

    /// <summary>5m 内有效敌数：实际敌数减去连击窗口内预计死亡的敌数。</summary>
    public static int GetEffectiveCount()
    {
        var total = (int)TargetHelper.EnemyInRange(5);
        var horizon = (ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切 ? 1 : 2) * ActionHelper.GetGcdTotal();
        return Math.Max(0, total - GunbreakerAoeTracker.CountDyingEnemies(horizon));
    }

    public bool EvaluateImmediate() => Compare(GetEffectiveCount());

    public bool EvaluateWait() => EvaluateImmediate();

    private bool Compare(int current)
    {
        if (!Enum.TryParse<CompareOperation>(_operation, out var operation))
            operation = CompareOperation.大于等于;

        return operation switch
        {
            CompareOperation.大于等于 => current >= _count,
            CompareOperation.大于 => current > _count,
            CompareOperation.等于 => current == _count,
            CompareOperation.小于 => current < _count,
            CompareOperation.小于等于 => current <= _count,
            _ => false,
        };
    }

    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string>
        {
            [CountKey] = _count.ToString(),
            [OperationKey] = _operation,
        },
    };

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, TypeKey, FromDto);
    }

    private static GunbreakerAoeCountCondition FromDto(ConditionDto dto)
    {
        var condition = new GunbreakerAoeCountCondition();
        if (dto.Params == null)
            return condition;

        if (dto.Params.TryGetValue(CountKey, out var count)) condition.SetParam(CountKey, count);
        if (dto.Params.TryGetValue(OperationKey, out var operation)) condition.SetParam(OperationKey, operation);

        return condition;
    }
}
