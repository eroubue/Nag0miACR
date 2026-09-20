namespace Nag0mi.Gunbreaker.Data;

internal static class GunbreakerQtDefaults
{
    public static IReadOnlyDictionary<string, bool> All { get; } = new Dictionary<string, bool>
    {
        ["爆发"] = true,
        ["倾泻爆发"] = false,
        ["AOE"] = true,
        ["无情"] = true,
        [GunbreakerQT.无情不延后] = true,
        ["子弹连"] = true,
        ["领域"] = true,
        ["音速破"] = true,
        ["优先音速破"] = false,
        ["弓形"] = true,
        ["血壤"] = true,
        ["爆发击"] = true,
        ["狮心连"] = true,
        ["倍攻"] = true,
        ["闪雷弹"] = true,
        ["命运之环"] = true,
        ["仅使用爆发击卸除子弹"] = false,
        ["小于3目标时不用弓形"] = false,
        ["弓形冲波允许错开无情"] = false,
        ["落地无情"] = false,
        ["血壤不延后"] = false,
        ["爆发药"] = true,
        ["优先狮心连"] = false,
        ["自动拉怪"] = true,
        ["自动减伤"] = true,
        ["强制盾姿"] = true,
    };

    // 出厂默认隐藏（QT 显隐从未配置过时由 Nag0miUISettings.IsQtVisible 回落到此;
    // 用户勾选/取消过即以其显式记录为准）。键一律用 GunbreakerQT 常量, 防拼写漂移。
    // 全模式默认隐藏：进阶/低频开关
    private static readonly HashSet<string> 默认隐藏 = new(StringComparer.Ordinal)
    {
        GunbreakerQT.优先狮心连, GunbreakerQT.血壤不延后, GunbreakerQT.落地无情,
        GunbreakerQT.弓形冲波允许错开无情, GunbreakerQT.小于3目标时不用弓形,
        GunbreakerQT.仅使用爆发击卸除子弹, GunbreakerQT.闪雷弹, GunbreakerQT.命运之环,
        GunbreakerQT.爆发击, GunbreakerQT.弓形, GunbreakerQT.领域, GunbreakerQT.音速破,
        GunbreakerQT.无情不延后, GunbreakerQT.无情, GunbreakerQT.倾泻爆发,
    };

    // 高难模式额外默认隐藏：辅助类开关
    private static readonly HashSet<string> 高难默认隐藏 = new(StringComparer.Ordinal)
    {
        GunbreakerQT.自动拉怪, GunbreakerQT.自动减伤, GunbreakerQT.强制盾姿,
    };

    // 某 QT 键在指定模式索引下的出厂默认显隐（true = 默认显示）。
    public static bool DefaultVisible(string key, int mode)
        => !默认隐藏.Contains(key)
           && (mode != (int)Control.GunbreakerMode.HighEnd || !高难默认隐藏.Contains(key));
}
