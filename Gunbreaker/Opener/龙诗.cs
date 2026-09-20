using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Rotation;

namespace Nag0mi.Gunbreaker.Opener;

// 龙诗战争 (DSR, Lv90)：无情延迟到第5GCD后，血壤第2GCD后早开，爆破领域开怪即用（FFLogs 7.5 前排 3/3 一致）。
// 无情后音速破优先，倍攻由 resolver 紧随其后；起手爆发药请手动（前排为第1GCD后吃）。
public class 龙诗 : IOpener
{
    public string OpenerName => "龙诗起手";

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
        new PAction(GunbreakerSkill.血壤, ActionType.OffGcd, ActionTargetType.Target)
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
        new PAction(GunbreakerSkill.残暴弹, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(GunbreakerSkill.无情, ActionType.OffGcd, ActionTargetType.Self)
        {
            RequiresVerification = true
        },
        new PAction(GunbreakerSkill.音速破, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
    };

    public void InitializeCountdown(CountDownHandler countdownHandler) => OpenerCountdown.Register(countdownHandler);
}
