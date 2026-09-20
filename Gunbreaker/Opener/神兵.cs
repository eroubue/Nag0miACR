using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Rotation;

namespace Nag0mi.Gunbreaker.Opener;

// 究极神兵 (UWU, Lv70)：危险领域开怪即用，123攒弹后第4GCD后无情（FFLogs 7.5 #2 打法）。
// 70级无血壤，烈牙弹药来自迅连斩；音速破由 resolver 自动插进烈牙-猛兽爪之间。
public class 神兵 : IOpener
{
    public string OpenerName => "神兵起手";

    public List<PAction> InCombatSequence => new()
    {
        new PAction(GunbreakerSkill.利刃斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(ActionHelper.GetAdjustedActionId(GunbreakerSkill.危险领域), ActionType.OffGcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(GunbreakerSkill.残暴弹, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(GunbreakerSkill.迅连斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(GunbreakerSkill.利刃斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(GunbreakerSkill.无情, ActionType.OffGcd, ActionTargetType.Self)
        {
            RequiresVerification = true
        },
    };

    public void InitializeCountdown(CountDownHandler countdownHandler) => OpenerCountdown.Register(countdownHandler);
}
