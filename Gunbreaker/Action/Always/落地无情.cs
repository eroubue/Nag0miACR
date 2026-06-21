using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Always;

public class 落地无情 : IDecisionResolver
{
    public CheckResult Check()
    {
        if (QT.QTGET(GunbreakerQT.停手))
        {
            return new CheckResult(false, "停止");
        }
        if (!GunbreakerHelper.IsReady(GunbreakerSkill.无情))
        {
            return new CheckResult(false, "无情未冷却");
        }
        if (QT.QTGET(GunbreakerQT.倾泻爆发))
        {
            return new CheckResult(true, "倾泻爆发");
        }
        if (!QT.QTGET(GunbreakerQT.爆发))
        {
            return new CheckResult(false, "爆发QT未开启");
        }
        if (!QT.QTGET(GunbreakerQT.无情))
        {
            return new CheckResult(false, "无情QT未开启");
        }
        if (!QT.QTGET(GunbreakerQT.落地无情))
        {
            return new CheckResult(false, "落地无情QT未开启");
        }

        return new CheckResult(true, "可释放落地无情");
    }

    public PAction GetAction()
    {
        return new PAction(GunbreakerSkill.无情, ActionType.Always, ActionTargetType.Self);
    }
}
