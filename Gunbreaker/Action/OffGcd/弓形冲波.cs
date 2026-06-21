using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;

namespace Nag0mi.Gunbreaker.Action.OffGcd
{
    public class 弓形冲波 : IDecisionResolver
    {
   
        public CheckResult Check()
        {
            if(GunbreakerHelper.通用检查())return new CheckResult(false, "通用检查未通过");
            if (Core.Target != null && Core.Target.DistanceToMe() >  5.0)
                return new CheckResult(false, "距离大于5米");
            if (ActionHelper.GetGcdRemain() <= 0.3&&!Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "GCD剩余不足");
            if (ActionHelper.GetGcdRemain() <= 0.6&&Core.Me.HasStatus(GunbreakerBuff.无情)) return new CheckResult(false, "无情中GCD剩余不足");
            var aoeCount = TargetHelper.EnemyInRange(5);
            if (!GunbreakerHelper.IsReady(GunbreakerSkill.弓形冲波))
            {
                return new CheckResult(false, "技能未就绪");
            }
            if (QT.QTGET(GunbreakerQT.停手))
            {
                return new CheckResult(false, "停手QT已开启");
            }

            if (QT.QTGET(GunbreakerQT.倾泻爆发))
            {
                return new CheckResult(true, "倾泻爆发QT开启");
            }
            if (!QT.QTGET(GunbreakerQT.爆发) || !QT.QTGET(GunbreakerQT.dot) || !QT.QTGET(GunbreakerQT.弓形))
            {
                return new CheckResult(false, "QT未开启");
            }

            if(Core.Me != null && !Core.Me.HasStatus(GunbreakerBuff.无情) && !QT.QTGET(GunbreakerQT.弓形冲波允许错开无情))return new CheckResult(false, "无情外不打");
        
            if (ActionHelper.RecentlyUsed(GunbreakerSkill.无情,1000)&&Core.Me != null && !Core.Me.HasStatus(GunbreakerBuff.无情) )return new CheckResult(false, "防止放了无情但没出buff");
        
            if (aoeCount < 3&&QT.QTGET(GunbreakerQT.小于3目标时不用弓形)) return new CheckResult(false, "小于3目标");
        
        

            return new CheckResult(true, "可释放弓形冲波");
        }



        public PAction GetAction()
        {
            return new PAction(GunbreakerSkill.弓形冲波, ActionType.OffGcd, ActionTargetType.Target);
        }
    
    }
}
