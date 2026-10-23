using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Extensions;
using PromeRotation.Helpers;

namespace Nag0mi.Gunbreaker;
public static class GunbreakerHelper
{
    // 结果按帧纪元缓存：同帧内所有 resolver 共享一次计算
    public static bool 通用检查() => GnbFrameCache.CommonCheck();

    // 同帧内同一技能的就绪判定只计算一次
    public static bool IsReady(uint actionId) => GnbFrameCache.IsReady(actionId, ComputeIsReady);

    private static bool ComputeIsReady(uint actionId)
    {
        var adjusted = GnbFrameCache.GetAdjustedActionId(actionId);
        if (adjusted == 0 || !ActionHelper.IsActionAvailableByLevelAndQuest(adjusted) ||Core.Me == null||GameData.IsPlayerOccupied()||ActionHelper.RecentlyUsed(adjusted,300))
        {
            return false;
        }
        if (ActionHelper.GetActionCharges(adjusted) >= 0.995f)
            return true;

        return ActionHelper.GetActionCooldown(adjusted)<=0.3;
    }

    
    /// <summary>
    /// 检测在自己的buff剩余时间内某个技能冷却能否结束
    /// </summary>
    /// <param name="statusId">要检查的buff ID</param>
    /// <param name="spellId">要检查的技能ID</param>
    /// <returns>true表示buff剩余时间内技能冷却能结束，false表示不能</returns>
    public static bool 技能冷却能否在buff剩余时间内结束(uint statusId, uint spellId)
    {
        
        if (Core.Me != null)
        {
            // 获取buff剩余时间（秒）（float）
            var buffTimeLeft = GnbFrameCache.GetStatusLeftTime(statusId);
       
        
            // 获取技能剩余冷却时间（秒）（float）
            var spellCooldown = ActionHelper.GetActionCooldown(spellId);
       
        
            // 如果技能没有冷却，直接返回true
            if (spellCooldown <= 0)
                return true;
        
            // 比较buff剩余时间和技能冷却时间
            return buffTimeLeft >= spellCooldown;
        }

        return false;
    }
    public static bool CoolDownInGCDs(this uint spellId, int count)
    {
        return ActionHelper.GetActionCooldown(spellId)<=(ActionHelper.GetGcdTotal() * count);
    }

    // 同帧内层数不变，按帧纪元缓存
    public static float 烈牙层数() => GnbFrameCache.FangCharges(ComputeFangCharges);

    private static float ComputeFangCharges()
    {
        var player = Core.Me;
        if (player == null) return 0f;
        float MaxCharges = 2f;  // 最大层数
        float PerCharge  = 30f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = GunbreakerSkill.烈牙.GetActionCooldown();
        
        // 等级适配
        if (player.Level < 60)
        {
            MaxCharges = 0f;
            //cdToFull -= 1 * PerCharge;
        }
        
        // 防止出界
        if (cdToFull < 0f) cdToFull = 0f;
        if (cdToFull > MaxCharges * PerCharge) cdToFull = MaxCharges * PerCharge;

        // 计算
        float real = MaxCharges - (cdToFull / PerCharge);
        if (real < 0f) real = 0f;
        if (real > MaxCharges) real = MaxCharges;

        return real;
    }
   

   
  
}
