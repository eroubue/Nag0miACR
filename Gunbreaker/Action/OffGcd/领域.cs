using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.OffGcd;

public class 领域 : IDecisionResolver
{
   
    public CheckResult Check()
    {
      
        if(GunbreakerHelper.通用检查())return new CheckResult(false, "通用检查未通过");
    

        if (!GunbreakerHelper.IsReady(ActionHelper.GetAdjustedActionId(GunbreakerSkill.危险领域)))
        {
            return new CheckResult(false, "危险领域未冷却");
        }
        if (QT.QTGET(GunbreakerQT.停手))
        {
            return new CheckResult(false, "停止");
        }
        if (QT.QTGET(GunbreakerQT.倾泻爆发))
        {
            return new CheckResult(false, "倾泻爆发QT已开启跳过检查");
        }
        if (!QT.QTGET(GunbreakerQT.爆发))
        {
            return new CheckResult(false, "爆发QT未开启");
        }
        if (!QT.QTGET(GunbreakerQT.领域))
        {
            return new CheckResult(false, "领域QT未开启");
        }
        if (GunbreakerSkill.无情.CoolDownInGCDs(2)&&QT.QTGET(GunbreakerQT.无情)&&QT.QTGET(GunbreakerQT.爆发)) return new CheckResult(false, "无情前2GCD不打");
        if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情,1000)&&!Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "防止放了无情但没出buff");
        if (ActionHelper.GetGcdRemain() <= 0.3&&!Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "GCD剩余不足");
        if (ActionHelper.GetGcdRemain() <= 0.6&&Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "无情中GCD剩余不足");
        return new CheckResult(true, "可释放领域");
            
    }

    public PAction GetAction()
    {
        return new PAction(ActionHelper.GetAdjustedActionId(GunbreakerSkill.危险领域), ActionType.OffGcd, ActionTargetType.Target);
    }
}
