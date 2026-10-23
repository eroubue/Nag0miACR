using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Gcd;

public class 音速破 : IDecisionResolver
{
    public CheckResult Check()
    {
        var player = Core.Me;
        if (player == null || GunbreakerHelper.通用检查())return new CheckResult(false, "通用检查未通过");

        if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情,1000)&&!GnbFrameCache.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "防止放了无情但没出buff");
        if (!GunbreakerHelper.IsReady(GunbreakerSkill.音速破)) return new CheckResult(false, "音速破未冷却");
        if(!GnbFrameCache.HasStatus(GunbreakerBuff.音速破预备))return new CheckResult(false, "无音速破预备buff");
       
        if (!QT.QTGET(GunbreakerQT.爆发)) return new CheckResult(false, "爆发QT未开启");
        if (!QT.QTGET(GunbreakerQT.音速破)) return new CheckResult(false, "音速破QT未开启");
        if(!QT.QTGET(GunbreakerQT.优先音速破)&&GnbFrameCache.HasStatus(GunbreakerBuff.无情)&&ActionHelper.GetActionCharges(GnbFrameCache.GetAdjustedActionId(GunbreakerSkill.烈牙))>=1.95)return new CheckResult(false, "优先子弹连");
        var aoecount = GnbFrameCache.EnemyInRange5();
        if ((int)aoecount >= GunbreakerAoe.SonicBreakBreakpoint(player.Level)) return new CheckResult(false, "目标数达到AOE平衡点不走单");

        return new CheckResult(true, "可释放音速破");
    }

    public PAction GetAction()
    {
        return new PAction(GunbreakerSkill.音速破, ActionType.Gcd, ActionTargetType.Target);
    }
}
