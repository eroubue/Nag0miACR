// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
namespace Nag0mi.Common.Data;

// 自定义热键的目标类型（纯逻辑，不依赖宿主 SDK，可单测）。
// 枚举顺序固定、只增不改：设置 JSON 按底层 int 序列化，重排会破坏旧配置。
public enum CustomHotkeyTarget
{
    Self,
    Target,
    TargetOfTarget,
    LowestHpTank,
    LowestHpHealer,
    LowestHpDps,
    LowestHpParty,
    DeadParty,
    MouseOver,
    Party2,
    Party3,
    Party4,
    Party5,
    Party6,
    Party7,
    Party8,
}

public static class CustomHotkeyTargets
{
    // 与枚举同序的显示标签（下拉框内容）
    public static readonly string[] Labels =
    [
        "自己", "目标", "目标的目标",
        "血量最低的坦克", "血量最低的奶妈", "血量最低的输出职业",
        "血量最低的队友", "死亡队友", "鼠标目标",
        "小队成员2", "小队成员3", "小队成员4", "小队成员5",
        "小队成员6", "小队成员7", "小队成员8",
    ];

    public static string Label(CustomHotkeyTarget target) => Labels[(int)target];

    // 可直接映射到宿主 ActionTargetType 的返回其底层 int
    // (Self=0, Target=1, TargetOfTarget=2, MouseOver=4, LowestHealthPartyMember=5, PartyMember2..8=6..12)；
    // 需自解析的（最低血量按职能/死亡队友）返回 null。以 int 返回保持本文件无 SDK 依赖。
    public static int? ToNativeTargetType(CustomHotkeyTarget target) => target switch
    {
        CustomHotkeyTarget.Self => 0,
        CustomHotkeyTarget.Target => 1,
        CustomHotkeyTarget.TargetOfTarget => 2,
        CustomHotkeyTarget.MouseOver => 4,
        CustomHotkeyTarget.LowestHpParty => 5,
        >= CustomHotkeyTarget.Party2 => 6 + ((int)target - (int)CustomHotkeyTarget.Party2),
        _ => null,
    };

    // 生成不与 existing 冲突的名字：重名时追加 ·2、·3…
    // （名字是面板排序/显隐/ImGui ID 的键，必须唯一）
    public static string GenerateUniqueName(string baseName, IEnumerable<string> existing)
    {
        var taken = new HashSet<string>(existing, StringComparer.Ordinal);
        if (!taken.Contains(baseName)) return baseName;
        for (var i = 2; ; i++)
        {
            var candidate = $"{baseName}·{i}";
            if (!taken.Contains(candidate)) return candidate;
        }
    }
}
