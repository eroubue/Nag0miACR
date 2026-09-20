using Nag0mi.Gunbreaker.Data;

namespace Nag0mi.Tests;

internal static class AoeBreakpointTests
{
    public static void Run()
    {
        // 基础 AOE 连：恶魔切未习得时不适用；10–39 为 4；40–95 为 3；96+（命运之印）为 2
        Check.Equal(int.MaxValue, GunbreakerAoe.BasicComboBreakpoint(9));
        Check.Equal(4, GunbreakerAoe.BasicComboBreakpoint(10));
        Check.Equal(4, GunbreakerAoe.BasicComboBreakpoint(25));
        Check.Equal(4, GunbreakerAoe.BasicComboBreakpoint(26));
        Check.Equal(4, GunbreakerAoe.BasicComboBreakpoint(39));
        Check.Equal(3, GunbreakerAoe.BasicComboBreakpoint(40));
        Check.Equal(3, GunbreakerAoe.BasicComboBreakpoint(71));
        Check.Equal(3, GunbreakerAoe.BasicComboBreakpoint(72));
        Check.Equal(3, GunbreakerAoe.BasicComboBreakpoint(85));
        Check.Equal(3, GunbreakerAoe.BasicComboBreakpoint(86));
        Check.Equal(3, GunbreakerAoe.BasicComboBreakpoint(95));
        Check.Equal(2, GunbreakerAoe.BasicComboBreakpoint(96));
        Check.Equal(2, GunbreakerAoe.BasicComboBreakpoint(100));

        // 命运之环：72级解锁后恒为 2
        Check.Equal(int.MaxValue, GunbreakerAoe.FatedCircleBreakpoint(71));
        Check.Equal(2, GunbreakerAoe.FatedCircleBreakpoint(72));
        Check.Equal(2, GunbreakerAoe.FatedCircleBreakpoint(85));
        Check.Equal(2, GunbreakerAoe.FatedCircleBreakpoint(86));
        Check.Equal(2, GunbreakerAoe.FatedCircleBreakpoint(96));
        Check.Equal(2, GunbreakerAoe.FatedCircleBreakpoint(100));

        // 子弹连：72级前不跳过；72–95 为 5；96+ 为 4
        Check.Equal(int.MaxValue, GunbreakerAoe.GnashingFangBreakpoint(71));
        Check.Equal(5, GunbreakerAoe.GnashingFangBreakpoint(72));
        Check.Equal(5, GunbreakerAoe.GnashingFangBreakpoint(95));
        Check.Equal(4, GunbreakerAoe.GnashingFangBreakpoint(96));
        Check.Equal(4, GunbreakerAoe.GnashingFangBreakpoint(100));

        // 音速破：54级前不适用；54–71 为 10；72–95 为 6；96+ 为 5
        Check.Equal(int.MaxValue, GunbreakerAoe.SonicBreakBreakpoint(53));
        Check.Equal(10, GunbreakerAoe.SonicBreakBreakpoint(54));
        Check.Equal(10, GunbreakerAoe.SonicBreakBreakpoint(71));
        Check.Equal(6, GunbreakerAoe.SonicBreakBreakpoint(72));
        Check.Equal(6, GunbreakerAoe.SonicBreakBreakpoint(95));
        Check.Equal(5, GunbreakerAoe.SonicBreakBreakpoint(96));
        Check.Equal(5, GunbreakerAoe.SonicBreakBreakpoint(100));

        // 循环威力 sanity：平衡点两侧单调
        Check.Near(435f, GunbreakerAoe.SingleTargetPerGcd(100));
        Check.True(GunbreakerAoe.AoeLoopPerGcd(100, 2) >= GunbreakerAoe.SingleTargetPerGcd(100));
        Check.True(GunbreakerAoe.AoeLoopPerGcd(85, 2) < GunbreakerAoe.SingleTargetPerGcd(85));
        Console.WriteLine("PASS: aoe breakpoints");
    }
}
