using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Gcd
{
  public class 倍攻 : IDecisionResolver
  {
    

    public CheckResult Check()
    {
      var player = Core.Me;
        if (player == null || GunbreakerHelper.通用检查())return new CheckResult(false, "通用检查未通过");
     
      if (Core.Target != null && Core.Target.DistanceToMe() >  5.0)
        return new CheckResult(false, "距离大于5米");
   
      if (!GunbreakerHelper.IsReady(GunbreakerSkill.倍攻)) return new CheckResult(false, "倍攻未冷却");
      if(!QT.QTGET(GunbreakerQT.倍攻))return new CheckResult(false, "倍攻QT未开启");
      if (!QT.QTGET(GunbreakerQT.爆发)) return new CheckResult(false, "爆发QT未开启");
      if (JobGaugeHelper.GNB.Ammo < 2) return new CheckResult(false, "晶壤小于2");
      if(!GnbFrameCache.HasStatus(GunbreakerBuff.无情)&& !QT.QTGET(GunbreakerQT.倾泻爆发))return new CheckResult(false, "无情外不打");
      if (GunbreakerSkill.无情.CoolDownInGCDs(2) && !QT.QTGET(GunbreakerQT.倾泻爆发)&&QT.QTGET(GunbreakerQT.无情)) return new CheckResult(false, "无情前2GCD不打倍攻");
      if (QT.QTGET(GunbreakerQT.仅使用爆发击卸除子弹)) return new CheckResult(false, "仅使用爆发击卸除子弹");
      var fangAdjusted = GnbFrameCache.GetAdjustedActionId(GunbreakerSkill.烈牙);
      var fangcd = ActionHelper.GetActionCooldown(fangAdjusted);//强制对齐子弹连cd，防止越来越延后
      if (fangAdjusted == GunbreakerSkill.烈牙&&fangcd <= 1&&QT.QTGET(GunbreakerQT.子弹连)&& ActionHelper.GetActionCharges(fangAdjusted)<1) return new CheckResult(false, "先打子弹连");//对齐子弹连
      if(GnbFrameCache.HasStatus(GunbreakerBuff.无情)&&ActionHelper.GetActionCharges(fangAdjusted)>=1.95&&player.Level==100&&!QT.QTGET(GunbreakerQT.优先狮心连))return new CheckResult(false, "优先子弹连");

     
      
      return new CheckResult(true, "可释放倍攻");
    }


    
    public PAction GetAction()
    {
      return new PAction(GunbreakerSkill.倍攻, ActionType.Gcd, ActionTargetType.Target);
    }
      
  }
}
