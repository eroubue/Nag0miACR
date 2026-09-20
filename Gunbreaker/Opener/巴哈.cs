using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Rotation;

namespace Nag0mi.Gunbreaker.Opener;

// 巴哈姆特 (UCoB, Lv70)：123 后裸打一套无无情子弹连（FFLogs 7.5 前排 3/3 完全一致）。
// 注意：前排的无情刻意延迟到 +26~29s 与爆发药绑定，单靠起手锁不住——
// 请配合时间轴在 +25s 前关闭「无情」QT、到点再打开，爆发药同时手动吃。
public class 巴哈 : IOpener
{
    public string OpenerName => "巴哈起手";

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
        new PAction(GunbreakerSkill.烈牙, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
    };

    public void InitializeCountdown(CountDownHandler countdownHandler) => OpenerCountdown.Register(countdownHandler);
}
