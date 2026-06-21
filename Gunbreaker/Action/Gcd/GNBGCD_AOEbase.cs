using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Gcd;

public class AOEbase : IDecisionResolver
{
    
    public CheckResult Check()
    {
        if(GunbreakerHelper.通用检查())return new CheckResult(false, "通用检查未通过");
        
        if (Core.Target != null && Core.Target.DistanceToMe() >  5.0)
            return new CheckResult(false, "距离大于5米");
        var aoeCount = TargetHelper.EnemyInRange(5);
        var AOE数 = GunbreakerSettings.Instance.AOE数;
     
        if (QT.QTGET(GunbreakerQT.停手)) return new CheckResult(false, "停止");
        if (!QT.QTGET(GunbreakerQT.AOE)) return new CheckResult(false, "AOEQT未开启");
        if (!GunbreakerHelper.IsReady(GunbreakerSkill.恶魔切)) return new CheckResult(false, "恶魔切未冷却");
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹) return new CheckResult(false, "连击中不打");
        if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情,1000)&&!Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "防止放了无情但没出buff");
       /* var fangcd = ActionHelper.GetActionCooldown(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)) * 1000f;//强制对齐子弹连cd，防止越来越延后
        if (ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) == GunbreakerSkill.烈牙&&fangcd <= 1000&&JobGaugeHelper.GNB.Ammo>=1&&QT.QTGET(GunbreakerQT.子弹连)&&QT.QTGET(GunbreakerQT.爆发)) return new CheckResult(false, "-103");//对齐子弹连*/
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切)
        {
            if (GunbreakerHelper.IsReady(GunbreakerSkill.恶魔杀) && Core.Me.Level >= 40) return new CheckResult(true, "可释放恶魔杀");
            return new CheckResult(false, "无连击不打");
        }

        if (Core.Me.HasStatus(4192) || Core.Me.HasStatus(4194)) return new CheckResult(false, "妖星乱舞绝境战，有应战buff不打");
        if (aoeCount >= AOE数) return new CheckResult(true, "可释放基础AOE");
        
        return new CheckResult(false, "AOE数量不足");
    }
    

    public PAction GetAction()
    {
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切 && ActionHelper.IsActionHighlighted(GunbreakerSkill.恶魔杀) && Core.Me.Level >= 40&&ActionHelper.GetComboLeftTime() >=0.1)
            return new PAction(ActionHelper.GetAdjustedActionId(GunbreakerSkill.恶魔杀), ActionType.Gcd, ActionTargetType.Target);
        return new PAction(ActionHelper.GetAdjustedActionId(GunbreakerSkill.恶魔切), ActionType.Gcd, ActionTargetType.Target);
        
    }
}
