namespace Nag0mi.Gunbreaker.Data;

/// <summary>
/// QT → 面板图标映射。IconId 默认是技能 id（经 IconHelper.GetActionIcon 取图标）；
/// IsGameIcon=true 时按原生游戏图标 id 加载。同一技能对应多个 QT 时用 Marker 角标区分。
/// </summary>
internal sealed record QtIcon(uint IconId, bool IsGameIcon = false, string? Marker = null);

internal static class GunbreakerQtIcons
{
    // 原生图标：问号兜底与爆发药（酊剂物品图标，效果可在游戏中微调）。
    private const uint FallbackQuestionIcon = 60071;
    private const uint TinctureIcon = 20601;

    public static IReadOnlyDictionary<string, QtIcon> Map { get; } = new Dictionary<string, QtIcon>
    {
        ["爆发"] = new(GunbreakerSkill.无情, Marker: "爆"),
        ["倾泻爆发"] = new(GunbreakerSkill.爆发击, Marker: "倾"),
        ["AOE"] = new(GunbreakerSkill.恶魔杀),
        ["无情"] = new(GunbreakerSkill.无情),
        [GunbreakerQT.无情不延后] = new(GunbreakerSkill.无情, Marker: "延"),
        ["子弹连"] = new(GunbreakerSkill.烈牙),
        ["领域"] = new(GunbreakerSkill.爆破领域),
        ["音速破"] = new(GunbreakerSkill.音速破),
        ["优先音速破"] = new(GunbreakerSkill.音速破, Marker: "优先"),
        ["弓形"] = new(GunbreakerSkill.弓形冲波),
        ["血壤"] = new(GunbreakerSkill.血壤),
        ["爆发击"] = new(GunbreakerSkill.爆发击),
        ["狮心连"] = new(GunbreakerSkill.崛起之心),
        ["倍攻"] = new(GunbreakerSkill.倍攻),
        ["闪雷弹"] = new(GunbreakerSkill.闪雷弹),
        ["命运之环"] = new(GunbreakerSkill.命运之环),
        ["仅使用爆发击卸除子弹"] = new(GunbreakerSkill.爆发击, Marker: "仅"),
        ["小于3目标时不用弓形"] = new(GunbreakerSkill.弓形冲波, Marker: "<3"),
        ["弓形冲波允许错开无情"] = new(GunbreakerSkill.弓形冲波, Marker: "自由"),
        ["落地无情"] = new(GunbreakerSkill.无情, Marker: "落地"),
        ["血壤不延后"] = new(GunbreakerSkill.血壤, Marker: "不延"),
        ["爆发药"] = new(TinctureIcon, IsGameIcon: true),
        ["优先狮心连"] = new(GunbreakerSkill.崛起之心, Marker: "优先"),
        ["自动拉怪"] = new(GunbreakerSkill.挑衅),
        ["自动减伤"] = new(GunbreakerSkill.铁壁),
        ["强制盾姿"] = new(GunbreakerSkill.盾姿),
    };

    // 缺映射的 QT（新增 key）退化为问号图标 + 首字角标，保证面板不崩。
    public static QtIcon For(string key) =>
        Map.TryGetValue(key, out var icon) ? icon : new QtIcon(FallbackQuestionIcon, true, key[..1]);

    // 供新框架 Nag0miUIJobEnv.QtIconResolver 直接注入。
    public static (uint iconId, bool isGameIcon, string? marker) ForUi(string qtKey)
    {
        var icon = For(qtKey);
        return (icon.IconId, icon.IsGameIcon, icon.Marker);
    }
}
