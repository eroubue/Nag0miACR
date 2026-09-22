using ECommons.Automation;
using Nag0mi.Gunbreaker.Data;
using PromeRotation.Data;
using PromeRotation.Managers;
using PromeRotation.Timeline.Actions;
using PromeRotation.Timeline.Core;

namespace Nag0mi.Gunbreaker.Timeline;

/// <summary>
/// 爆发药 QT 门禁：覆盖时间轴内置「使用爆发药」行为的创建委托。
/// 执行时仅当本 ACR 生效且「爆发药」QT 开启才真正用药；
/// 未开启时不执行，并输出日志「爆发药QT未开启」。
/// 其它 ACR 生效时完全透传原始行为，互不影响。
/// </summary>
internal static class GunbreakerPotionGate
{
    private const string PotionTypeKey = "usepotion";

    // 在职业节点注册时调用：先确保内置节点完成注册，再覆盖为门禁版本。
    internal static void Install()
    {
        TimelineRegistrations.Bootstrap();
        ActionFactory.Register(PotionTypeKey, Create);
    }

    private static IAction Create(ActionDto dto)
    {
        var mode = Enum.TryParse(dto.Mode, true, out PotionUseMode parsed) ? parsed : PotionUseMode.Enqueue;
        return new QtGatedPotionAction(new UsePotionAction(mode));
    }

    private sealed class QtGatedPotionAction : IAction, ISerializableAction, ITimelineExecutionDiagnostic
    {
        private readonly UsePotionAction _inner;
        private string? _failure;

        public QtGatedPotionAction(UsePotionAction inner) => _inner = inner;

        public void Execute()
        {
            _failure = null;
            if (RotationManager.GetCurrentRotation() is GunbreakerRotation
                && !PromeSettings.Instance.GetQt(GunbreakerQT.爆发药))
            {
                _failure = "爆发药QT未开启";
                Chat.ExecuteCommand("/e [Timeline] 爆发药QT未开启");
                return;
            }

            _inner.Execute();
        }

        public ActionDto ToDto() => _inner.ToDto();

        bool ITimelineExecutionDiagnostic.TryConsumeExecutionFailure(out string message)
        {
            if (_failure != null)
            {
                message = _failure;
                _failure = null;
                return true;
            }

            // 透传原始行为的执行诊断（如无药可用等）
            return ((ITimelineExecutionDiagnostic)_inner).TryConsumeExecutionFailure(out message);
        }
    }
}
