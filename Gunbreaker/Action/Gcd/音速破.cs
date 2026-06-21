using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Gcd;

public class 音速破 : IDecisionResolver
{
    private static readonly 倍攻 s_doubledownSlot = new 倍攻();
    public CheckResult Check()
    {
        if(GunbreakerHelper.通用检查())return new CheckResult(false, "通用检查未通过");

        if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情,1000)&&!Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "防止放了无情但没出buff");
        if (!GunbreakerHelper.IsReady(GunbreakerSkill.音速破)) return new CheckResult(false, "音速破未冷却");
        if(!Core.Me.HasStatus(GunbreakerBuff.音速破预备))return new CheckResult(false, "无音速破预备buff");
       
        if (QT.QTGET(GunbreakerQT.停手)) return new CheckResult(false, "停止");
        if (!QT.QTGET(GunbreakerQT.爆发)) return new CheckResult(false, "爆发QT未开启");
        if (!QT.QTGET(GunbreakerQT.dot)) return new CheckResult(false, "dotQT未开启");
        if (!QT.QTGET(GunbreakerQT.音速破)) return new CheckResult(false, "音速破QT未开启");
        if(!QT.QTGET(GunbreakerQT.优先音速破)&&Core.Me.HasStatus(GunbreakerBuff.无情)&&ActionHelper.GetActionCharges(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙))>=1.95)return new CheckResult(false, "优先子弹连");
        var aoecount = TargetHelper.EnemyInRange(5);
        if (Core.Me.Level >=72 && aoecount >= 5) return new CheckResult(false, "AOE数量大于5不走单");

        return new CheckResult(true, "可释放音速破");
    }

    public PAction GetAction()
    {
        return new PAction(GunbreakerSkill.音速破, ActionType.Gcd, ActionTargetType.Target);
    }
}
