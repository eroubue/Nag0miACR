using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace Nag0mi.Gunbreaker
{
    public class GunbreakerRotationEventHandler : IRotationEventHandler
    {
        // 每帧都会执行一次下面的方法
        public void OnUpdate()
        {
            
        }

        // 非战斗状态下每帧都会执行一次下面的方法
        public void OnOutOfBattleUpdate()
        {
            
        }
        
        // 交战状态变为True的时候会执行一次下面的代码
        public void OnBattleStarted()
        {
            
        }
        
        // 这里战斗中每帧都会执行一次下面的方法
        public void OnBattleUpdate()
        {
            
        }

        public void OnNoTarget()
        {
            
        }
        
        // 交战状态变为False的时候会执行一次下面的代码
        public void OnBattleEnded()
        {
            GunbreakerBattleData.Instance.Reset();
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
        }

        // 切换区域会执行一次下面的方法
        public void OnTerritoryChanged(ushort territoryId)
        {
            GunbreakerBattleData.Instance.Reset();
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
        }
    }
}
