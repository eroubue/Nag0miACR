using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.OffGcd;

public class 血壤 : IDecisionResolver
{
   
    public CheckResult Check()
    {
     
        if (ActionHelper.GetGcdRemain() <= 0.3) return new CheckResult(false, "GCD剩余不足");
        if (QT.QTGET(GunbreakerQT.停手))
        {
            return new CheckResult(false, "停止");
        }
        if (!GunbreakerHelper.IsReady(GunbreakerSkill.血壤))
        {
            return new CheckResult(false, "血壤未冷却");
        }
        if (!QT.QTGET(GunbreakerQT.爆发))
        {
            return new CheckResult(false, "爆发QT未开启");
        }
        if (!QT.QTGET(GunbreakerQT.血壤))
        {
            return new CheckResult(false, "血壤QT未开启");
        }


        return new CheckResult(true, "可释放血壤");
    }


    public PAction GetAction()
    {
        return new PAction(GunbreakerSkill.血壤, ActionType.OffGcd, ActionTargetType.Target);
    }
    
}
