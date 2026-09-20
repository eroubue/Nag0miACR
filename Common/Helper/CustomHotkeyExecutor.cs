// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.ExcelServices;
using Nag0mi.Common.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;

namespace Nag0mi.Common.Helper;

// 自定义热键的执行：按配置的目标类型解析出实际目标后排入宿主热键队列。
// 宿主执行口（ActionUpdater.TryExecuteAction）总是先按 PAction.Target 做一次解析、解析不到就放弃；
// NetworkTid != 0 时再用它覆盖目标 id。因此自解析目标统一写 Target=Self（必可解析）+ NetworkTid=目标实体 id
// （普通单位实体 id 低 32 位有效，uint 截断安全）。
internal static class CustomHotkeyExecutor
{
    public static void Fire(CustomHotkeyEntry e)
    {
        // 可直接映射宿主 ActionTargetType 的交给宿主解析
        if (CustomHotkeyTargets.ToNativeTargetType(e.Target) is { } native)
        {
            HotkeyQueueManager.TryEnqueue(new PAction(e.SkillId, e.Type, (ActionTargetType)native));
            return;
        }

        var target = ResolveCustom(e.Target);
        if (target == null)
        {
            HintHelper.ShowToast2($"{e.Name}：没有有效目标", 2, HintHelper.HintType.Info);
            return;
        }

        HotkeyQueueManager.TryEnqueue(new PAction(e.SkillId, e.Type, ActionTargetType.Self)
        {
            NetworkTid = (uint)target.EntityId,
        });
    }

    // LowestHpTank/Healer/Dps：小队（含自己, 与 GetParty 语义一致）中存活且职能匹配者按血量百分比升序取第一；
    // DeadParty：小队中第一个死亡的非自己成员（自己已死时队列本就拒发）。
    private static IBattleChara? ResolveCustom(CustomHotkeyTarget target)
    {
        var party = PartyHelper.GetParty();
        switch (target)
        {
            case CustomHotkeyTarget.DeadParty:
            {
                var meId = Core.Me?.EntityId;
                return party.FirstOrDefault(c => c.IsDead && c.EntityId != meId);
            }
            case CustomHotkeyTarget.LowestHpTank:
                return LowestByHp(party, c => HealerHelper.IsTank((Job)c.ClassJob.RowId));
            case CustomHotkeyTarget.LowestHpHealer:
                return LowestByHp(party, c => HealerHelper.IsHealer((Job)c.ClassJob.RowId));
            case CustomHotkeyTarget.LowestHpDps:
                return LowestByHp(party, c => c.IsPlayer()
                    && !HealerHelper.IsTank((Job)c.ClassJob.RowId)
                    && !HealerHelper.IsHealer((Job)c.ClassJob.RowId));
            default:
                return null;
        }
    }

    private static IBattleChara? LowestByHp(List<IBattleChara> party, Func<IBattleChara, bool> roleMatch)
        => party.Where(c => !c.IsDead && roleMatch(c))
            .OrderBy(PartyHelper.GetHpPercent)
            .FirstOrDefault();
}
