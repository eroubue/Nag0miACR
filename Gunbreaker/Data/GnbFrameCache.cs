using ECommons.DalamudServices;
using PromeRotation.Core;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Data;

/// <summary>
/// 帧级快照：同一纪元内共享昂贵的宿主查询结果（目标扫描、状态列表、技能可用性等），
/// 避免各 resolver 之间及状态汇报路径对同一数据的重复计算。
/// 纪元使用宿主 Framework 更新时刻，同一更新内共享快照，下一次更新自动失效。
/// </summary>
internal static class GnbFrameCache
{
    private static long Epoch => Svc.Framework.LastUpdateUTC.Ticks;

    // 通用检查（目标/距离/占用状态）
    private static long _commonCheckEpoch = -1;
    private static bool _commonCheck;

    public static bool CommonCheck()
    {
        var epoch = Epoch;
        if (epoch == _commonCheckEpoch) return _commonCheck;
        _commonCheck = ComputeCommonCheck();
        _commonCheckEpoch = epoch;
        return _commonCheck;
    }

    private static bool ComputeCommonCheck()
    {
        var currentAttackRange = GameData.GetCurrentMeleeRange();
        var player = Core.Me;
        return player == null
               || Core.Target == null
               || Core.Target.EntityId == player.EntityId
               || Core.Target.IsPlayer()
               || Core.Target.DistanceToMe() > currentAttackRange
               || GameData.IsPlayerOccupied();
    }

    // 自身 5m 内敌数（全量目标表扫描，每纪元一次）
    private static long _aoeCountEpoch = -1;
    private static float _aoeCount5;

    public static float EnemyInRange5()
    {
        var epoch = Epoch;
        if (epoch == _aoeCountEpoch) return _aoeCount5;
        _aoeCount5 = TargetHelper.EnemyInRange(5);
        _aoeCountEpoch = epoch;
        return _aoeCount5;
    }

    // 技能动态 ID（连击/变体技能在一帧内不变）
    private static long _adjustedEpoch = -1;
    private static readonly Dictionary<uint, uint> _adjusted = new();

    public static uint GetAdjustedActionId(uint actionId)
    {
        var epoch = Epoch;
        if (epoch != _adjustedEpoch)
        {
            _adjustedEpoch = epoch;
            _adjusted.Clear();
        }
        if (_adjusted.TryGetValue(actionId, out var adjusted)) return adjusted;
        return _adjusted[actionId] = ActionHelper.GetAdjustedActionId(actionId);
    }

    // 自身状态快照（每纪元一次性遍历状态列表；语义与逐次扫描一致：
    // 存在性按 StatusId 匹配，剩余时间取 RemainingTime 的绝对值，不存在返回 0）
    private static long _statusEpoch = -1;
    private static readonly HashSet<uint> _statuses = new();
    private static readonly Dictionary<uint, float> _statusLeftTime = new();
    private static bool _statusSnapshotReady;

    public static bool HasStatus(uint statusId)
    {
        EnsureStatusSnapshot();
        return _statuses.Contains(statusId);
    }

    public static float GetStatusLeftTime(uint statusId)
    {
        EnsureStatusSnapshot();
        return _statusLeftTime.TryGetValue(statusId, out var left) ? left : 0f;
    }

    private static void EnsureStatusSnapshot()
    {
        var epoch = Epoch;
        if (epoch == _statusEpoch && _statusSnapshotReady) return;
        _statusSnapshotReady = false;
        _statuses.Clear();
        _statusLeftTime.Clear();
        var me = Core.Me;
        if (me != null)
        {
            foreach (var status in me.StatusList)
            {
                _statuses.Add(status.StatusId);
                var left = status.RemainingTime;
                // 同名状态取列表中的首条，与逐次查询一致。
                _statusLeftTime.TryAdd(status.StatusId, left < 0 ? Math.Abs(left) : left);
            }
        }
        _statusEpoch = epoch;
        _statusSnapshotReady = true;
    }

    // 技能就绪判定（按传入技能 ID memo，每纪元有效）
    private static long _readyEpoch = -1;
    private static readonly Dictionary<uint, bool> _ready = new();

    public static bool IsReady(uint actionId, Func<uint, bool> compute)
    {
        var epoch = Epoch;
        if (epoch != _readyEpoch)
        {
            _readyEpoch = epoch;
            _ready.Clear();
        }
        if (_ready.TryGetValue(actionId, out var ready)) return ready;
        return _ready[actionId] = compute(actionId);
    }

    // 烈牙层数（每纪元一次）
    private static long _fangChargesEpoch = -1;
    private static float _fangCharges;

    public static float FangCharges(Func<float> compute)
    {
        var epoch = Epoch;
        if (epoch == _fangChargesEpoch) return _fangCharges;
        _fangCharges = compute();
        _fangChargesEpoch = epoch;
        return _fangCharges;
    }

    // resolver Check 结果 memo：同纪元内 Next*/状态汇报/嵌套调用共享同一结果，
    // 顺带缓存 resolver 显示名，避免每帧取 Type.Name
    private static long _checkEpoch = -1;
    private static readonly Dictionary<IDecisionResolver, CheckResult> _checkMemo = new(ReferenceEqualityComparer.Instance);
    private static readonly Dictionary<Type, string> _resolverNames = new();

    public static CheckResult CheckCached(this IDecisionResolver resolver)
    {
        var epoch = Epoch;
        if (epoch != _checkEpoch)
        {
            _checkEpoch = epoch;
            _checkMemo.Clear();
        }
        if (_checkMemo.TryGetValue(resolver, out var entry)) return entry;
        var result = resolver.Check();
        _checkMemo[resolver] = result;
        return result;
    }

    public static string NameCached(this IDecisionResolver resolver)
    {
        var type = resolver.GetType();
        if (_resolverNames.TryGetValue(type, out var name)) return name;
        return _resolverNames[type] = type.Name;
    }
}
