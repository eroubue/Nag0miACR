using PromeRotation.Timeline.Core;

namespace Nag0mi.Gunbreaker.Timeline;

/// <summary>
/// 绝枪职业的时间轴专属条件 / 行为节点提供者。
/// 由 GunbreakerRotation 通过静态属性 NodeProvider 暴露。
/// </summary>
public sealed class GunbreakerJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        // 覆盖内置「使用爆发药」行为：接入本 ACR 的爆发药 QT 门禁
        GunbreakerPotionGate.Install();
        GunbreakerQtAction.Register(context);
        GunbreakerSettingsAction.Register(context);
        GunbreakerHotkeyAction.Register(context);
        GunbreakerShotBlacklistAction.Register(context);
        GunbreakerAmmoCondition.Register(context);
        GunbreakerComboCondition.Register(context);
    }

    public IReadOnlyList<(string DisplayName, string Description, Func<ICondition> Create)> GetConditionDescriptors()
        => new (string, string, Func<ICondition>)[]
        {
            ("绝枪/检测量谱-子弹比较", "按比较符判断当前子弹数量", () => new GunbreakerAmmoCondition()),
            ("绝枪/检测连击进度", "判断上一个连击 GCD 的进度(1-3)", () => new GunbreakerComboCondition()),
        };

    public IReadOnlyList<(string DisplayName, string Description, Func<IAction> Create)> GetActionDescriptors()
        => new (string, string, Func<IAction>)[]
        {
            ("绝枪/QT", "批量设置绝枪 QT 开关", () => new GunbreakerQtAction()),
            ("绝枪/设置", "修改绝枪战斗设置", () => new GunbreakerSettingsAction()),
            ("绝枪/热键", "触发已注册的绝枪热键", () => new GunbreakerHotkeyAction()),
            ("绝枪/闪雷弹黑名单管理", "管理闪雷弹黑名单 DataId", () => new GunbreakerShotBlacklistAction()),
        };
}
