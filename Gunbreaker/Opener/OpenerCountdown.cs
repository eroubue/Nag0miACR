using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Rotation;

namespace Nag0mi.Gunbreaker.Opener;

// 绝本起手共用的倒计时动作：4.5s 按 MT/ST 与「强制盾姿」QT 管理盾姿，0.3s 按「突进起手」设置突进或闪雷弹开怪。
internal static class OpenerCountdown
{
    public static void Register(CountDownHandler countdownHandler)
    {
        if (Core.Me.HasStatus(GunbreakerBuff.王室亲卫) && GunbreakerSettings.Instance.ST && QT.QTGET(GunbreakerQT.强制盾姿))
            countdownHandler.AddAction(
                4500,
                new PAction(GunbreakerSkill.关盾姿, ActionType.OffGcd, ActionTargetType.Self)
                {
                    RequiresVerification = true
                });

        if (Core.Me.HasStatus(GunbreakerBuff.王室亲卫) && !GunbreakerSettings.Instance.ST && QT.QTGET(GunbreakerQT.强制盾姿))
            countdownHandler.AddAction(
                4500,
                new PAction(GunbreakerSkill.盾姿, ActionType.OffGcd, ActionTargetType.Self)
                {
                    RequiresVerification = true
                });

        if (GunbreakerSettings.Instance.突进起手)
            countdownHandler.AddAction(
                300,
                new PAction(GunbreakerSkill.弹道, ActionType.OffGcd, ActionTargetType.Target)
                {
                    RequiresVerification = true
                });
        else
            countdownHandler.AddAction(
                300,
                new PAction(GunbreakerSkill.闪雷弹, ActionType.Gcd, ActionTargetType.Target)
                {
                    RequiresVerification = true
                });
    }
}
