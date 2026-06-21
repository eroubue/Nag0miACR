using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Gcd;

public class 爆发击 : IDecisionResolver
{
    private static readonly 子弹连 s_gnashingFangSlot = new 子弹连();
    private static readonly 狮心连 s_lionHeartSlot = new 狮心连();
    private static readonly 倍攻 s_doubledownSlot = new 倍攻();

    public CheckResult Check()
    {
        if(GunbreakerHelper.通用检查())return new CheckResult(false, "通用检查未通过");

        if (JobGaugeHelper.GNB.Ammo == 0) return new CheckResult(false, "无晶壤");
        
        if (QT.QTGET(GunbreakerQT.停手)) return new CheckResult(false, "停止");
        if (!QT.QTGET(GunbreakerQT.爆发击)) return new CheckResult(false, "爆发击QT未开启");
        if (!GunbreakerHelper.IsReady(GunbreakerSkill.爆发击)) return new CheckResult(false, "技能未冷却");
        if (s_gnashingFangSlot.Check().Success)
            return new CheckResult(false, "子弹连优先");
        if (s_lionHeartSlot.Check().Success)
            return new CheckResult(false, "狮心连优先");
        if (s_doubledownSlot.Check().Success)
            return new CheckResult(false, "倍攻优先");
        if ((ActionHelper.GetAdjustedActionId(GunbreakerSkill.崛起之心)==GunbreakerSkill.支配之心||ActionHelper.GetAdjustedActionId(GunbreakerSkill.崛起之心)==GunbreakerSkill.终结之心))
            return new CheckResult(false, "正在狮心连里不打");
        if (((ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) == GunbreakerSkill.猛兽爪 || ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) == GunbreakerSkill.凶禽爪)))
            return new CheckResult(false, "正在子弹连里不打");
        var aoeCount = TargetHelper.EnemyInRange(5);
        if (aoeCount >= 2 && GunbreakerHelper.IsReady(GunbreakerSkill.命运之环)&&QT.QTGET(GunbreakerQT.AOE)&&!Core.Me.HasStatus(4192) &&! Core.Me.HasStatus(4194)) return new CheckResult(false, "AOE中优先命运之环");

        if (QT.QTGET(GunbreakerQT.仅使用爆发击卸除子弹)&&JobGaugeHelper.GNB.Ammo >= (byte) GunbreakerSettings.Instance.保留子弹数+1) return new CheckResult(true, "卸除子弹");
        if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情,1000)&&!Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "防止放了无情但没出buff");

        if(Core.Me.HasStatus(GunbreakerBuff.Medicated)&&Core.Me.HasStatus(GunbreakerBuff.无情))return new CheckResult(true, "无情+药打");
        // 等级判断逻辑
        if (Core.Me.Level == 100)
        {
            if (Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "无情中不打");

            if (JobGaugeHelper.GNB.Ammo == 3 &&
                ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹) return new CheckResult(true, "溢出时打");
            if (JobGaugeHelper.GNB.Ammo == 3 &&
                ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切) return new CheckResult(true, "溢出时打");
            if (QT.QTGET(GunbreakerQT.无情)&&ActionHelper.GetActionCooldown(GunbreakerSkill.无情) * 1000f <= 10000 && ActionHelper.GetLastComboID() == GunbreakerSkill.迅连斩 && JobGaugeHelper.GNB.Ammo != 1)return new CheckResult(true, "无情前10秒打爆发击");
            if (QT.QTGET(GunbreakerQT.无情)&&ActionHelper.GetActionCooldown(GunbreakerSkill.无情) * 1000f <= 2500 && ActionHelper.GetLastComboID() == GunbreakerSkill.迅连斩 )return new CheckResult(true, "无情前2.5秒打爆发击");

            return new CheckResult(false, "不打");
        }

        if (Core.Me.Level >= 88 && Core.Me.Level < 100)
        {
            if (Core.Me.HasStatus(GunbreakerBuff.无情))
            {
                if  (ActionHelper.GetActionCharges(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)) >=1) return new CheckResult(false, "保留子弹不打爆发击");
                if (JobGaugeHelper.GNB.Ammo == 1 && ((GunbreakerHelper.技能冷却能否在buff剩余时间内结束(GunbreakerBuff.无情, GunbreakerSkill.烈牙)&&aoeCount < 4)||GunbreakerHelper.技能冷却能否在buff剩余时间内结束(GunbreakerBuff.无情, GunbreakerSkill.倍攻))) return new CheckResult(false, "保留子弹不打爆发击");
            }
            else{
                if (JobGaugeHelper.GNB.Ammo == 3 &&
                     ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹) return new CheckResult(true, "溢出时打");
                if (JobGaugeHelper.GNB.Ammo == 3 &&
                    ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切) return new CheckResult(true, "溢出时打");
            }
            return new CheckResult(false, "不打");
        }

        if (Core.Me.Level < 88)
        {
            if (JobGaugeHelper.GNB.Ammo == 2 &&
                ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹) return new CheckResult(true, "溢出时打");
            if (JobGaugeHelper.GNB.Ammo == 2 &&
                ActionHelper.GetLastComboID() == GunbreakerSkill.恶魔切) return new CheckResult(true, "溢出时打");
            if (Core.Me.HasStatus(GunbreakerBuff.无情)&&Core.Me.Level >= 60)
            {
                if (JobGaugeHelper.GNB.Ammo == 1 && GunbreakerHelper.技能冷却能否在buff剩余时间内结束(GunbreakerBuff.无情, GunbreakerSkill.烈牙)) return new CheckResult(false, "保留子弹不打爆发击");
                if  (ActionHelper.GetActionCharges(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)) >=1) return new CheckResult(false, "保留子弹不打爆发击");
                return new CheckResult(true, "无情中打爆发击");
            }

            if (Core.Me.HasStatus(GunbreakerBuff.无情) && Core.Me.Level < 60&&ActionHelper.GetActionCharges(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)) <1) return new CheckResult(true, "无情中打");
        }

        return new CheckResult(false, "不打");
    }

    public PAction GetAction()
    {
        return new PAction(GunbreakerSkill.爆发击, ActionType.Gcd, ActionTargetType.Target);
    }
}
