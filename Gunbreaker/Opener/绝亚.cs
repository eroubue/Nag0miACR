using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace Nag0mi.Gunbreaker.Opener;

// 亚历山大 (TEA, Lv80)：血壤第1GCD后，无情固定第3GCD(迅连斩)后（FFLogs 7.5 前排 3/3 近乎逐帧一致）。
public class 绝亚 : IOpener
{
    public string OpenerName => "绝亚起手";

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
        new PAction(GunbreakerSkill.迅连斩, ActionType.Gcd, ActionTargetType.Target)
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
