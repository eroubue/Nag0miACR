using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.Gcd
{
    public class 闪雷弹 : IDecisionResolver
    {
        public CheckResult Check()
        {
            var target = Core.Target;
            if (target == null) return new CheckResult(false, "无目标");
            if (GunbreakerBattleData.Instance.ShotBlackList.Contains(target.BaseId))
                return new CheckResult(false, "目标为闪雷弹黑名单");
            if (Core.Target != null && Core.Target.DistanceToMe() <  6.5)
                return new CheckResult(false, "距离小于6.5米");
            
            // 闪雷弹仅用于远程拉怪，目标进入近战范围时不使用
           
            if (!GunbreakerHelper.IsReady(GunbreakerSkill.闪雷弹)) return new CheckResult(false, "技能未就绪");
            
            if (QT.QTGET(GunbreakerQT.停手)) return new CheckResult(false, "停止");
            if (!QT.QTGET(GunbreakerQT.闪雷弹)) return new CheckResult(false, "闪雷弹QT未开启");
            if (ActionHelper.RecentlyUsed(GunbreakerSkill.弹道,1000)) return new CheckResult(false, "一秒内使用过突进");
            return new CheckResult(true, "可释放闪雷弹");
        }

        public PAction GetAction()
        {
            return new PAction(GunbreakerSkill.闪雷弹, ActionType.Gcd, ActionTargetType.Target);
        }
    }
}
