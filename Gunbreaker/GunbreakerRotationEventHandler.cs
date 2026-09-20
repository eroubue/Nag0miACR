using ECommons.DalamudServices;
using Nag0mi.Common.UI;
using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace Nag0mi.Gunbreaker
{
    public class GunbreakerRotationEventHandler : IRotationEventHandler
    {
        private bool wasDead;

        // 每帧都会执行一次下面的方法
        public void OnUpdate()
        {
            if (!Nag0miUIFramework.Active)
            {
                wasDead = false;
                return;
            }

            var dead = Svc.Objects.LocalPlayer?.IsDead == true;
            if (dead && !wasDead && GunbreakerRotation.ActiveProfiles?.Config.ResetQtOnDeath == true)
            {
                // 恢复当前模式的 QT 默认值（语义同旧版 ResetCurrentModeQtToDefaults：
                // 框架按模式快照恢复，快照缺省的键回落出厂默认）
                var defaults = Nag0mi.Common.Data.Nag0miUISettings.Instance.GetCurrentModeDefaults();
                foreach (var (key, fallback) in GunbreakerQtDefaults.All)
                    Nag0miUIFramework.设置QT(key, defaults.TryGetValue(key, out var v) ? v : fallback);
            }
            wasDead = dead;
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
            GunbreakerAoeTracker.Reset();
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
        }

        // 切换区域会执行一次下面的方法
        public void OnTerritoryChanged(ushort territoryId)
        {
            GunbreakerBattleData.Instance.Reset();
            GunbreakerAoeTracker.Reset();
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
        }
    }
}
