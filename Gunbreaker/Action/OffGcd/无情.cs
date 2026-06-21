using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.OffGcd;

public class 无情 : IDecisionResolver
{
    
    public CheckResult Check()
    {
        
        if (ActionHelper.GetGcdRemain()<= 0.65) return new CheckResult(false, "GCD剩余不足");
        if (QT.QTGET(GunbreakerQT.停手)) return new CheckResult(false, "停止");
        
        if (!GunbreakerHelper.IsReady(GunbreakerSkill.无情)) return new CheckResult(false, "无情未冷却");
        if (QT.QTGET(GunbreakerQT.倾泻爆发)) return new CheckResult(true, "倾泻爆发");
        if (!QT.QTGET(GunbreakerQT.爆发)||!QT.QTGET(GunbreakerQT.无情)) return new CheckResult(false, "无情QT或爆发QT未开启");
        
        return new CheckResult(true, "可释放无情");
            
    }
    public PAction GetAction()
    {
        return new PAction(GunbreakerSkill.无情, ActionType.OffGcd, ActionTargetType.Target);
    }
}
