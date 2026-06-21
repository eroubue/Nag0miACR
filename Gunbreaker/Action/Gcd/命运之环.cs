using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Gcd;

public class 命运之环 : IDecisionResolver
{
    

    public CheckResult Check()
    {
        if(GunbreakerHelper.通用检查())return new CheckResult(false, "通用检查未通过");
       
        if (Core.Target != null && Core.Target.DistanceToMe() >  5.0)
            return new CheckResult(false, "距离大于5米");
       
        if (!GunbreakerHelper.IsReady(GunbreakerSkill.命运之环)) return new CheckResult(false, "命运之环未冷却");
        if (QT.QTGET(GunbreakerQT.停手))  return new CheckResult(false, "停止"); 
        if (!QT.QTGET(GunbreakerQT.AOE)) return new CheckResult(false, "AOEQT未开启");
        if (!QT.QTGET(GunbreakerQT.命运之环)) return new CheckResult(false, "命运之环QT未开启");
        
        if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情,1000)&&!Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "防止放了无情但没出buff");
        var aoeCount = TargetHelper.EnemyInRange(5);
        var AOE数 = GunbreakerSettings.Instance.AOE数;
        if (aoeCount < AOE数) return new CheckResult(false, "AOE数量不足");
        if (Core.Me.HasStatus(4192) || Core.Me.HasStatus(4194)) return new CheckResult(false, "妖星乱舞绝境战，有应战buff不打");
        
        if (Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(true, "无情中打");
        // 等级判断逻辑
        if (Core.Me.Level == 100)
        {
            if (JobGaugeHelper.GNB.Ammo == 3 &&
                ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹) return new CheckResult(true, "溢出时打");
            if (JobGaugeHelper.GNB.Ammo == 3 &&
                ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切) return new CheckResult(true, "溢出时打");
            if ((GunbreakerSkill.无情.CoolDownInGCDs(5) && ActionHelper.GetLastComboID() == GunbreakerSkill.迅连斩 && JobGaugeHelper.GNB.Ammo != 1 ))return new CheckResult(true, "无情前5GCD");
            
            return new CheckResult(false, "不打");
        }

        if (Core.Me.Level >= 88 && Core.Me.Level < 100)
        {
            if (JobGaugeHelper.GNB.Ammo == 3 &&
                ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹) return new CheckResult(true, "溢出时打");
            if (JobGaugeHelper.GNB.Ammo == 3 &&
                ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切) return new CheckResult(true, "溢出时打");
        }
        else if (Core.Me.Level < 88)
        {
            if (JobGaugeHelper.GNB.Ammo == 2 &&
                ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹) return new CheckResult(true, "溢出时打");
            if (JobGaugeHelper.GNB.Ammo == 2 &&
                ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切) return new CheckResult(true, "溢出时打");
        }

        if (QT.QTGET(GunbreakerQT.仅使用爆发击卸除子弹)) return new CheckResult(false, "仅使用爆发击卸除子弹");
        /*if (!Core.Me.HasAura(Buffs.无情) && Core.Me.HasAura(Buffs.Medicated) &&
            GunbreakerHelper.IsReady(GunbreakerSkill.无情)) return new CheckResult(false, "-21");//吃药还没放无情不打
        var fangcd = ActionHelper.GetActionCooldown(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙));//强制对齐子弹连cd，防止越来越延后
        if (JobGaugeHelper.GNB.Ammo == 1 &&
            Core.Me.HasMyAuraWithTimeleft(Buffs.无情, (int)fangcd)&&aoeCount < 3&&QT.QTGET(GunbreakerQT.子弹连)&&QT.QTGET(GunbreakerQT.爆发)) return new CheckResult(false, "-45");//在有一颗子弹而且子弹连能在无情内转好时优先子弹连*/
        if (JobGaugeHelper.GNB.Ammo == 1 &&aoeCount < 3&&QT.QTGET(GunbreakerQT.子弹连)&&QT.QTGET(GunbreakerQT.爆发)) return new CheckResult(false, "保留子弹优先子弹连");


        
        return new CheckResult(true, "可释放命运之环");
    }

    public PAction GetAction()
    {
        return new PAction(GunbreakerSkill.命运之环, ActionType.Gcd, ActionTargetType.Target);
    }

}
