using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using PromeRotation.Extensions;

namespace Nag0mi.Gunbreaker.Data;

/// <summary>
/// 追踪 AOE 范围（自身 5m）内敌人的血量变化，估算剩余存活时间（TTK）。
/// 供「敌人血量过低、AOE 普通连打不完」时及时切回单体连击使用。
/// </summary>
public static class GunbreakerAoeTracker
{
    private const float Range = 5f;
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Expire = TimeSpan.FromSeconds(10);
    private const double MinWindowSec = 0.5; // 样本跨度太短不估算，避免噪声

    private sealed class Tracked
    {
        public readonly List<(DateTime Time, uint Hp)> Samples = new();
        public DateTime LastSeen;
    }

    private static readonly Dictionary<ulong, Tracked> _tracked = new();
    private static readonly List<(ulong Id, uint Hp)> _inRange = new();
    private static readonly List<ulong> _expiredKeys = new();
    private static DateTime _lastSample = DateTime.MinValue;

    /// <summary>5m 内预计活不过 horizonSec 的敌人数；没有掉血数据的敌人按能存活计（开怪不误判）。</summary>
    public static int CountDyingEnemies(double horizonSec)
    {
        SampleIfDue();
        var dying = 0;
        foreach (var (id, hp) in _inRange)
            if (EstimateTtk(id, hp) <= horizonSec)
                dying++;
        return dying;
    }

    public static void Reset()
    {
        _tracked.Clear();
        _inRange.Clear();
        _lastSample = DateTime.MinValue;
    }

    private static void SampleIfDue()
    {
        var now = DateTime.UtcNow;
        if (now - _lastSample < SampleInterval) return;
        _lastSample = now;

        _inRange.Clear();
        try
        {
            foreach (var obj in Svc.Objects)
            {
                if (obj is not IBattleChara b || !b.IsEnemy() || b.IsDead || b.DistanceToMe() > Range)
                    continue;
                _inRange.Add((b.EntityId, b.CurrentHp));
                var entry = _tracked.TryGetValue(b.EntityId, out var e) ? e : _tracked[b.EntityId] = new Tracked();
                entry.Samples.Add((now, b.CurrentHp));
                entry.Samples.RemoveAll(s => now - s.Time > Window);
                entry.LastSeen = now;
            }
        }
        catch
        {
            _inRange.Clear();
        }

        // 过期条目两阶段手动清理，避免每帧 LINQ 分配（闭包 + 枚举器 + List）
        if (_tracked.Count > 0)
        {
            _expiredKeys.Clear();
            foreach (var kv in _tracked)
                if (now - kv.Value.LastSeen > Expire)
                    _expiredKeys.Add(kv.Key);
            foreach (var id in _expiredKeys)
                _tracked.Remove(id);
        }
    }

    private static double EstimateTtk(ulong id, uint currentHp)
    {
        if (!_tracked.TryGetValue(id, out var entry) || entry.Samples.Count < 2)
            return double.PositiveInfinity;
        var oldest = entry.Samples[0];
        var newest = entry.Samples[^1];
        var dt = (newest.Time - oldest.Time).TotalSeconds;
        if (dt < MinWindowSec) return double.PositiveInfinity;
        var dps = ((double)oldest.Hp - newest.Hp) / dt;
        if (dps <= 0) return double.PositiveInfinity; // 满血/回血/锁血：按存活计
        return currentHp / dps;
    }
}
