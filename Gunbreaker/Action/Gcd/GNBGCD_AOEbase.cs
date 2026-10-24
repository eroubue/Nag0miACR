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
        // 当前等级下 AOE 基础连威力不低于单体 123 的最小目标数（按已习得技能威力自动计算）
        var breakpoint = GunbreakerAoe.BasicComboBreakpoint(Core.Me.Level);
        // 连击窗口内预计会死的敌人不计入：打不完的 AOE 连击没有收益
        var horizon = (ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切 ? 1 : 2) * ActionHelper.GetGcdTotal();
        var effectiveCount = Math.Max(0, (int)aoeCount - GunbreakerAoeTracker.CountDyingEnemies(horizon));
     
        if (!QT.QTGET(GunbreakerQT.AOE)) return new CheckResult(false, "AOEQT未开启");
        if (!GunbreakerHelper.IsReady(GunbreakerSkill.恶魔切)) return new CheckResult(false, "恶魔切未冷却");
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹) return new CheckResult(false, "连击中不打");
        if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情,1000)&&!Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "防止放了无情但没出buff");
       /* var fangcd = ActionHelper.GetActionCooldown(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)) * 1000f;//强制对齐子弹连cd，防止越来越延后
        if (ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) == GunbreakerSkill.烈牙&&fangcd <= 1000&&JobGaugeHelper.GNB.Ammo>=1&&QT.QTGET(GunbreakerQT.子弹连)&&QT.QTGET(GunbreakerQT.爆发)) return new CheckResult(false, "-103");//对齐子弹连*/
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切)
        {
            // 有效敌数已低于平衡点（敌人将死/数量不足）：恶魔杀落点无收益，中断改打单体
            if (effectiveCount < breakpoint) return new CheckResult(false, "AOE收益不足，中断连击");
            if (GunbreakerHelper.IsReady(GunbreakerSkill.恶魔杀) && Core.Me.Level >= 40) return new CheckResult(true, "可释放恶魔杀");
            return new CheckResult(false, "无连击不打");
        }

        if (Core.Me.HasStatus(4192) || Core.Me.HasStatus(4194)) return new CheckResult(false, "妖星乱舞绝境战，有应战buff不打");
        if (effectiveCount >= breakpoint) return new CheckResult(true, "可释放基础AOE");
        
        return new CheckResult(false, effectiveCount < aoeCount ? "敌人即将死亡，AOE连击无收益" : "AOE数量不足");
    }
    

    public PAction GetAction()
    {
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切 && ActionHelper.IsActionHighlighted(GunbreakerSkill.恶魔杀) && Core.Me.Level >= 40&&ActionHelper.GetComboLeftTime() >=0.1)
            return new PAction(ActionHelper.GetAdjustedActionId(GunbreakerSkill.恶魔杀), ActionType.Gcd, ActionTargetType.Target);
        return new PAction(ActionHelper.GetAdjustedActionId(GunbreakerSkill.恶魔切), ActionType.Gcd, ActionTargetType.Target);
        
    }
}
