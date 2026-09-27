using ECommons.DalamudServices;
using Nag0mi.Common.UI;
using Nag0mi.Gunbreaker.Control;
using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Rotation;
using PromeRotation.Timeline;
using PromeRotation.Timeline.Core;

namespace Nag0mi.Gunbreaker
{
    public class GunbreakerRotationEventHandler : IRotationEventHandler
    {
        private readonly GunbreakerRotation _rotation;
        private bool wasDead;
        private TimelineMetadata? lastTimelineMeta;

        public GunbreakerRotationEventHandler(GunbreakerRotation rotation)
        {
            _rotation = rotation;
        }

        // 每帧都会执行一次下面的方法
        public void OnUpdate()
        {
            // 宿主无时间轴加载事件，轮询 CurrentMeta 引用变化：换到非空时间轴即视为加载完成
            var meta = TimelineManager.CurrentMeta;
            if (!ReferenceEquals(meta, lastTimelineMeta))
            {
                lastTimelineMeta = meta;
                if (meta != null)
                    EnterHighEndPrep();
            }

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

            // 满编（8 人小队）进入绝境战地图时自动进入高难备战状态
            if (HighEndDutyTerritories.Contains(territoryId) && PartyHelper.GetParty().Count == 8)
                EnterHighEndPrep();
        }

        // 切到高难模式、关闭停手、关闭自动攻击（幂等）
        private void EnterHighEndPrep()
        {
            _rotation.ChangeMode(GunbreakerMode.HighEnd);
            PromeSettings.Instance.EnableAcr = AcrState.On;
            PromeSettings.Instance.AutoPull = false;
        }
    }
}
