using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Rotation;

namespace Nag0mi.Gunbreaker.Opener;

public class 无情2g : IOpener
{
    public string OpenerName => "无情2g起手";

    public List<PAction> InCombatSequence => new()
    {
        new PAction(GunbreakerSkill.利刃斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(GunbreakerSkill.血壤, ActionType.OffGcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(GunbreakerSkill.残暴弹, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(GunbreakerSkill.无情, ActionType.OffGcd, ActionTargetType.Self)
        {
            RequiresVerification = true
        },
        
    };

    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
   
        if (Core.Me.HasStatus(GunbreakerBuff.王室亲卫)&&GunbreakerSettings.Instance.ST&&QT.QTGET(GunbreakerQT.强制盾姿))
            countdownHandler.AddAction(
                4500,
                new PAction(GunbreakerSkill.关盾姿, ActionType.OffGcd, ActionTargetType.Self)
                {
                    RequiresVerification = true
                });
        
        if (Core.Me.HasStatus(GunbreakerBuff.王室亲卫)&&!GunbreakerSettings.Instance.ST&&QT.QTGET(GunbreakerQT.强制盾姿))
            countdownHandler.AddAction(
                4500,
                new PAction(GunbreakerSkill.盾姿, ActionType.OffGcd, ActionTargetType.Self)
                {
                    RequiresVerification = true
                });
        if (QT.QTGET(GunbreakerQT.突进起手))

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
