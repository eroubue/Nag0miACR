namespace Nag0mi.Gunbreaker.Data;

/// <summary>
/// 按当前等级已习得技能/特性的威力，自动计算 AOE 相对单体的盈亏平衡目标数。
/// 模型：稳态循环每 GCD 威力，子弹的生成（迅连斩/恶魔杀）与消耗（爆发击/命运之环）均计入。
/// 威力来源：官方职业指南（7.2+）；补丁调整威力时只需更新这里的常量。
/// </summary>
public static class GunbreakerAoe
{
    // 习得等级
    private const int BrutalShellLevel = 4;
    private const int DemonSliceLevel = 10;
    private const int SolidBarrelLevel = 26;
    private const int BurstStrikeLevel = 30;
    private const int DemonSlaughterLevel = 40;
    private const int SonicBreakLevel = 54;
    private const int ContinuationLevel = 70;
    private const int FatedCircleLevel = 72;
    private const int HypervelocityLevel = 86;
    private const int FatedBrandLevel = 96;

    // 威力（连击技能按连击成功计）
    private const float KeenEdgePotency = 300f;
    private const float BrutalShellPotency = 380f;
    private const float SolidBarrelPotency = 460f;
    private const float DemonSlicePotency = 100f;
    private const float DemonSlaughterPotency = 160f;
    private const float BurstStrikePotency = 420f;
    private const float HypervelocityPotency = 180f;
    private const float GnashingFangPotency = 440f;
    private const float SavageClawPotency = 500f;
    private const float WickedTalonPotency = 560f;
    private const float JugularRipPotency = 220f;
    private const float AbdomenTearPotency = 260f;
    private const float EyeGougePotency = 300f;
    private const float FatedCirclePotency = 300f;
    private const float FatedBrandPotency = 120f;
    private const float SonicBreakPotency = 340f + 120f * 5f; // 直击 + DoT 全额跳完

    /// <summary>爆发击 + 超音速续剑（86级起）的单体威力。</summary>
    public static float BurstStrikeValue(int level) =>
        BurstStrikePotency + (level >= HypervelocityLevel ? HypervelocityPotency : 0f);

    /// <summary>命运之环 + 命运之印续剑（96级起）的每目标威力。</summary>
    public static float FatedCircleValue(int level) =>
        FatedCirclePotency + (level >= FatedBrandLevel ? FatedBrandPotency : 0f);

    /// <summary>单体循环每 GCD 威力：3 GCD 基础连 + 1 GCD 爆发击（消耗基础连产出的子弹）。</summary>
    public static float SingleTargetPerGcd(int level)
    {
        if (level < BrutalShellLevel) return KeenEdgePotency;
        if (level < SolidBarrelLevel) return (KeenEdgePotency + BrutalShellPotency) / 2f;
        if (level < BurstStrikeLevel)
            return (KeenEdgePotency + BrutalShellPotency + SolidBarrelPotency) / 3f;
        return (KeenEdgePotency + BrutalShellPotency + SolidBarrelPotency + BurstStrikeValue(level)) / 4f;
    }

    /// <summary>AOE 循环每 GCD 总威力：恶魔切 + 恶魔杀 + 1 GCD 子弹消耗（72级前只能打单体爆发击）。</summary>
    public static float AoeLoopPerGcd(int level, int targets)
    {
        if (level < DemonSliceLevel || targets <= 0) return 0f;
        if (level < DemonSlaughterLevel) return DemonSlicePotency * targets;
        var combo = (DemonSlicePotency + DemonSlaughterPotency) * targets;
        var spend = level >= FatedCircleLevel ? FatedCircleValue(level) * targets : BurstStrikeValue(level);
        return (combo + spend) / 3f;
    }

    /// <summary>基础 AOE 连（恶魔切/恶魔杀）威力不低于单体 123 的最小目标数。</summary>
    public static int BasicComboBreakpoint(int level) => Breakpoint(level, SingleTargetPerGcd(level));

    /// <summary>命运之环每目标威力不低于爆发击的最小目标数；72级前无命运之环，返回 MaxValue。</summary>
    public static int FatedCircleBreakpoint(int level) =>
        level < FatedCircleLevel
            ? int.MaxValue
            : SmallestN(n => FatedCircleValue(level) * n >= BurstStrikeValue(level));

    /// <summary>AOE 循环威力不低于子弹连（含续剑）的最小目标数；72级前沿用现状不跳过子弹连。</summary>
    public static int GnashingFangBreakpoint(int level)
    {
        if (level < FatedCircleLevel) return int.MaxValue;
        var fangPerGcd = (GnashingFangPotency + SavageClawPotency + WickedTalonPotency
            + (level >= ContinuationLevel ? JugularRipPotency + AbdomenTearPotency + EyeGougePotency : 0f)) / 3f;
        return Breakpoint(level, fangPerGcd);
    }

    /// <summary>AOE 循环威力不低于音速破（DoT 跳满）的最小目标数；54级前无音速破，返回 MaxValue。</summary>
    public static int SonicBreakBreakpoint(int level) =>
        level < SonicBreakLevel ? int.MaxValue : Breakpoint(level, SonicBreakPotency);

    private static int Breakpoint(int level, float singleTargetPerGcd) =>
        SmallestN(n => AoeLoopPerGcd(level, n) >= singleTargetPerGcd);

    private static int SmallestN(Func<int, bool> aoeWins)
    {
        for (var n = 1; n <= 32; n++)
            if (aoeWins(n)) return n;
        return int.MaxValue;
    }
}
