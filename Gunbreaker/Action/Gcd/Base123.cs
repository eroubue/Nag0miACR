using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Gcd;

public class Base123 : IDecisionResolver
{
   
    public CheckResult Check()
    {
        if(GunbreakerHelper.通用检查())return new CheckResult(false, "通用检查未通过");
        
       
        
        if (QT.QTGET(GunbreakerQT.停手)) return new CheckResult(false, "停止");
        if (!GunbreakerHelper.IsReady(GunbreakerSkill.利刃斩)) return new CheckResult(false, "利刃斩未冷却");
        if (((ActionHelper.GetAdjustedActionId(36937U) == 36938U || ActionHelper.GetAdjustedActionId(36937U) == 36939U)))
            return new CheckResult(false, "正在狮心连里不打123");//正在狮心连里不打123
        if (((ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) == GunbreakerSkill.猛兽爪 || ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) == GunbreakerSkill.凶禽爪)))
            return new CheckResult(false, "正在子弹连里不打123");//正在子弹连里不打123
        
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹 && GunbreakerSkill.迅连斩.IsActionHighlighted() &&
            JobGaugeHelper.GNB.Ammo == 2 &&
            Core.Me != null &&
            Core.Me.Level<88) return new CheckResult(false, "溢出不打准备卸子弹");
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹 && GunbreakerSkill.迅连斩.IsActionHighlighted() &&
            JobGaugeHelper.GNB.Ammo == 3 &&
            Core.Me != null &&
            Core.Me.Level >= 88) return new CheckResult(false, "溢出不打准备卸子弹");//溢出不打准备卸子弹
        if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情,1000) && Core.Me != null && !Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "防止放了无情但没出buff就打");//防止放了无情但没出buff就打
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切 && Core.Me != null && Core.Me.Level<100) return new CheckResult(false, "小于一百级没狮心连时不中断aoe连击");
       // if (ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹 && ActionHelper.GetComboLeftTime() < 5 && !Core.Me.HasAura(Buffs.无情)) return new CheckResult(true, "26");
        //炒股模式3续剑-奇数分钟3弹开始打无情前子弹连，偶数分钟1弹开始打无情前子弹连

        
       /* var fangcd = ActionHelper.GetActionCooldown(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)) * 1000f;//强制对齐子弹连cd，防止越来越延后
        if (ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) == GunbreakerSkill.烈牙&&fangcd <= 1000&&JobGaugeHelper.GNB.Ammo>=1&&QT.QTGET(GunbreakerQT.子弹连)&&QT.QTGET(GunbreakerQT.爆发)) return new CheckResult(false, "-103");//对齐子弹连*/
        return new CheckResult(true, "允许打123");
    }
    

    public PAction GetAction()
    {
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹&&Core.Me.Level >= 26&&ActionHelper.GetComboLeftTime() >=0.1) return new PAction(GunbreakerSkill.迅连斩, ActionType.Gcd, ActionTargetType.Target);
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.利刃斩&&ActionHelper.GetComboLeftTime() >=0.1) return new PAction(GunbreakerSkill.残暴弹, ActionType.Gcd, ActionTargetType.Target);
        
       
        return new PAction(GunbreakerSkill.利刃斩, ActionType.Gcd, ActionTargetType.Target);
    }
}
