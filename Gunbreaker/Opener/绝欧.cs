using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace Nag0mi.Gunbreaker.Opener;

// 绝欧米茄 (TOP, Lv90)：血壤+无情随第1GCD开，倍攻做第2个GCD（FFLogs 7.5 前排多数派）。
public class 绝欧 : IOpener
{
    public string OpenerName => "绝欧起手";

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
        new PAction(GunbreakerSkill.无情, ActionType.OffGcd, ActionTargetType.Self)
        {
            RequiresVerification = true
        },
        new PAction(GunbreakerSkill.倍攻, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
    };

    public void InitializeCountdown(CountDownHandler countdownHandler) => OpenerCountdown.Register(countdownHandler);
}
