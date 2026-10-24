using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Gcd
{
  public class 狮心连 : IDecisionResolver
  {
    public CheckResult Check()
    {
      if(GunbreakerHelper.通用检查())return new CheckResult(false, "通用检查未通过");

      if (!GunbreakerHelper.IsReady(GunbreakerSkill.崛起之心)) return new CheckResult(false, "崛起之心未冷却");
      if (!QT.QTGET(GunbreakerQT.爆发))
        return new CheckResult(false, "爆发QT未开启");
      if (!QT.QTGET(GunbreakerQT.狮心连)) { return new CheckResult(false, "狮心连QT未开启"); }
      
      if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情,1000)&&!Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "防止放了无情但没出buff");
      if (Core.Me.GetStatusLeftTime(GunbreakerBuff.心有灵狮) > ActionHelper.GetActionCooldown(GunbreakerSkill.无情)) return new CheckResult(false, "狮心连Buff剩余时间大于无情冷却不使用");
      if (ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)== GunbreakerSkill.猛兽爪||ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)== GunbreakerSkill.凶禽爪)
        return new CheckResult(false, "子弹连中不打");
      if (ActionHelper.GetAdjustedActionId(GunbreakerSkill.崛起之心)==GunbreakerSkill.支配之心||ActionHelper.GetAdjustedActionId(GunbreakerSkill.崛起之心)==GunbreakerSkill.终结之心)
        return new CheckResult(true, "继续打狮心连");
      if (!Core.Me.HasStatus(GunbreakerBuff.心有灵狮)&ActionHelper.GetAdjustedActionId(GunbreakerSkill.崛起之心)==GunbreakerSkill.崛起之心) return new CheckResult(false, "无狮心连Buff");
      //if (!Core.Me.HasStatus(Buffs.无情)&&ActionHelper.GetAdjustedActionId(GunbreakerSkill.崛起之心) == GunbreakerSkill.崛起之心) return new CheckResult(false, "-41");
      if (Core.Me.HasStatus(GunbreakerBuff.无情) &&Core.Me.GetStatusLeftTime(GunbreakerBuff.无情)<13&&
          ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) == GunbreakerSkill.烈牙 ) return new CheckResult(true, "优先打狮心连");
      if (!QT.QTGET((GunbreakerQT.优先狮心连))&&QT.QTGET(GunbreakerQT.子弹连)
                                  &&GunbreakerHelper.IsReady(GunbreakerSkill.烈牙)&&JobGaugeHelper.GNB.Ammo!=0&&Core.Me.GetStatusLeftTime(GunbreakerBuff.无情)>=13) return new CheckResult(false, "子弹连优先");
      if (!Core.Me.HasStatus(GunbreakerBuff.无情) && Core.Me.HasStatus(GunbreakerBuff.Medicated) &&
          GunbreakerHelper.IsReady(GunbreakerSkill.无情)) return new CheckResult(false, "吃药还没放无情不打");

      return new CheckResult(true, "可释放狮心连");
    }

    public PAction GetAction()
    {
      return new PAction(
          ActionHelper.GetAdjustedActionId(GunbreakerSkill.崛起之心),
          ActionType.Gcd,
          ActionTargetType.Target);
    }
  }
}
