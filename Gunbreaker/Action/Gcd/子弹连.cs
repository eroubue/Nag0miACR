using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Gcd;

public class 子弹连 : IDecisionResolver
{


    private static readonly 狮心连 s_lionHeartSlot = new 狮心连();

    public CheckResult Check()
    {
        if (GunbreakerHelper.通用检查()) return new CheckResult(false, "通用检查未通过");
        if (ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)==0) return new CheckResult(false, "烈牙技能id未获取");
        /*if (!ActionHelper.IsActionAvailableByLevelAndQuest(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙))) return new CheckResult(false, "烈牙未解锁");
        
        if (ActionHelper.RecentlyUsed(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙),300)) return new CheckResult(false, "烈牙刚放");
        if (GunbreakerHelper.烈牙层数() < 1f) return new CheckResult(false, "烈牙层数小于1");*/
        if (!GunbreakerHelper.IsReady (GunbreakerSkill.烈牙)) return new CheckResult(false, "烈牙未就绪");

        if (QT.QTGET(GunbreakerQT.停手)) return new CheckResult(false, "停止");

        if (!QT.QTGET(GunbreakerQT.爆发)) return new CheckResult(false, "爆发QT未开启");
        if (JobGaugeHelper.GNB.Ammo == 0&&ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) == GunbreakerSkill.烈牙) return new CheckResult(false, "无晶壤");
        //if (QT.QTGET(GunbreakerQT.倾泻爆发)&& JobGaugeHelper.GNB.Ammo >= (byte) GNBSettings.Instance.保留子弹数+1&&!QT.QTGET(GunbreakerQT.仅使用爆发击卸除子弹)) return new CheckResult(true, "10");
        if (QT.QTGET(GunbreakerQT.仅使用爆发击卸除子弹)) return new CheckResult(false, "仅使用爆发击卸除子弹");
        if (!QT.QTGET(GunbreakerQT.子弹连)) return new CheckResult(false, "子弹连QT未开启");
        if (QT.QTGET(GunbreakerQT.优先狮心连) && s_lionHeartSlot.Check().Success) return new CheckResult(false, "狮心连优先");
        if (ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) == GunbreakerSkill.烈牙 &&
            Core.Me.HasStatus(GunbreakerBuff.心有灵狮) &&
            Core.Me.GetStatusLeftTime(GunbreakerBuff.心有灵狮) <= 7.5) return new CheckResult(false, "狮心连即将buff过期,不打子弹连");
        if (ActionHelper.GetAdjustedActionId(36937U) == 36938U || ActionHelper.GetAdjustedActionId(36937U) == 36939U)
            return new CheckResult(false, "正在狮心连里不打子弹连");
        //if (JobGaugeHelper.GNB.Ammo >=2&&Core.Me.HasAura(Buffs.无情) && (GunbreakerHelper.IsReady(GunbreakerSkill.倍攻)||GunbreakerSkill.倍攻.CoolDownInGCDs(1))&&QT.QTGET(GunbreakerQT.倍攻)) 
        //   return new CheckResult(false, "-331");//进无情之后打了子弹连进CD之后先打倍攻防止空转 1.满子弹连充能且倍攻满充能进无情需要先打子弹连1再进行后续 2.无情前已经打了子弹连1，无情内应该先打倍攻&&GunbreakerHelper.烈牙层数() <=1.75f&&ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)!= GunbreakerSkill.烈牙

        if (Core.Me.HasStatus(GunbreakerBuff.无情) &&
            ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) != GunbreakerSkill.烈牙 &&
            GunbreakerHelper.IsReady(GunbreakerSkill.音速破) && QT.QTGET(GunbreakerQT.dot) && QT.QTGET(GunbreakerQT.音速破)&&Core.Me.HasStatus(GunbreakerBuff.音速破预备))
            return new CheckResult(false, "优先音速破");



        var aoeCount = TargetHelper.EnemyInRange(5);
        if (Core.Me.Level >= 72 && aoeCount >= 3 && GunbreakerHelper.IsReady(GunbreakerSkill.命运之环) &&
            QT.QTGET(GunbreakerQT.AOE) && QT.QTGET(GunbreakerQT.命运之环)) //敌人数量大于3不打子弹连
            return new CheckResult(false, "AOE数量大于3优先命运之环");
        if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情, 1000) && !Core.Me.HasStatus(GunbreakerBuff.无情))
            return new CheckResult(false, "防止放了无情但没出buff");
        //if (QT.QTGET(GunbreakerQT.音速破) && !Helper.自身存在Buff大于时间(Buffs.音速破预备, 5000)) return new CheckResult(false, "-51");
        //上面这个这样写会有bug啥都不打不要用
        // 开场3连击结束前优先放3再放子弹连
        if (ActionHelper.GetLastComboID() == GunbreakerSkill.残暴弹 && ActionHelper.GetComboLeftTime() < 2.5 &&
            !Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "连击即将结束优先完成3连击");
        if (ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)== GunbreakerSkill.猛兽爪||ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)== GunbreakerSkill.凶禽爪)
            return new CheckResult(true, "继续打子弹连");



        // 等级判断逻辑
        if (Core.Me.Level == 100)
        {
           

            var action = ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙);
            var comboStep = ActionHelper.GetLastComboID();


            if (ActionHelper.GetActionCooldown(GunbreakerSkill.无情)  < 7.5 && GunbreakerHelper.烈牙层数() > 1.5f &&
                ActionHelper.GetActionCooldown(GunbreakerSkill.无情)  <=
                ActionHelper.GetActionCooldown(GunbreakerSkill.烈牙))
                return new CheckResult(true, "无情前3GCD打子弹连");
            if (GunbreakerHelper.烈牙层数() >= 1.75f)
                return new CheckResult(true, "充能溢出打子弹连");
            // if (nmGcds == 4 && 子弹连充能层数 > 1.5f && comboStep == GunbreakerSkill.残暴弹 && ammo == 3 && action == GunbreakerSkill.烈牙&&ActionHelper.GetActionCooldown(GunbreakerSkill.无情) * 1000f>ActionHelper.GetActionCooldown(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙)) * 1000f)//无情cd大于子弹连cd 
            //    return new CheckResult(true, "123523");                // 无情前4GCD ammo3
            if (ActionHelper.GetActionCooldown(GunbreakerSkill.无情)  < 5 && action == GunbreakerSkill.凶禽爪 &&
                comboStep == GunbreakerSkill.残暴弹)
                return new CheckResult(false, "打3不打子弹连");
        }
        else if (Core.Me.Level >= 88 && Core.Me.Level < 100)
        {

            if (GunbreakerHelper.烈牙层数() >=
                1.75f) return new CheckResult(true, "充能溢出打子弹连");
        }
        else if (Core.Me.Level < 88)
        {

            if (GunbreakerHelper.烈牙层数() >=
                1.75f) return new CheckResult(true, "充能溢出打子弹连");
        }

        if (Core.Me.HasStatus(GunbreakerBuff.无情) && Core.Me.GetStatusLeftTime(GunbreakerBuff.无情) <= 13 &&
            ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙) == GunbreakerSkill.烈牙 &&
            GunbreakerHelper.IsReady(GunbreakerSkill.崛起之心) && QT.QTGET(GunbreakerQT.狮心连)&&Core.Me.HasStatus(GunbreakerBuff.心有灵狮))
            return new CheckResult(false, "优先狮心连");
        if (Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(true, "无情中放子弹连");




        return new CheckResult(false, "不打子弹连");





    }

    public PAction GetAction()
    {
        return new PAction(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙), ActionType.Gcd,
            ActionTargetType.Target);
    }
}