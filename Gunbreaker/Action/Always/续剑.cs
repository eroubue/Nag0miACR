using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Always;

public class 续剑 : IDecisionResolver
{
    public CheckResult Check()
    {
        if(GunbreakerHelper.通用检查())return new CheckResult(false, "通用检查未通过");
        if (ActionHelper.GetAdjustedActionId(GunbreakerSkill.续剑) == GunbreakerSkill.续剑) return new CheckResult(false, "未获得续剑");
        if (!ActionHelper.GetAdjustedActionId(GunbreakerSkill.续剑).IsActionHighlighted())
        {
            return new CheckResult(false, "当前不可使用");
        }
        if (QT.QTGET(GunbreakerQT.停手))
        {
            return new CheckResult(false, "停手QT");
        }
        if ((GunbreakerHelper.IsReady(GunbreakerSkill.无情)||GunbreakerHelper.AbilityCoolDownInNextXGcdsWindow(GunbreakerSkill.无情,0)&&ActionHelper.GetGcdRemain() >= 0.6)&&QT.QTGET(GunbreakerQT.无情)&&QT.QTGET(GunbreakerQT.爆发))
        {
            return new CheckResult(false, "无情已冷却优先放无情");
        }
        if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情,1000)&&!Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "防止放了无情但没出buff");
        if (ActionHelper.GetGcdRemain() <= 0.3&&!Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "GCD剩余不足");
        return new CheckResult(true, "可释放续剑");
    }



    public PAction GetAction()
    {
        return new PAction(ActionHelper.GetAdjustedActionId(GunbreakerSkill.续剑), ActionType.Always, ActionTargetType.Target);
    }
}