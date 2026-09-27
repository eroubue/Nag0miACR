namespace Nag0mi.Gunbreaker.Data;

// 五个绝境战副本地图（TerritoryId）。满编（8 人小队）进入这些地图时自动进入高难备战状态。
internal static class HighEndDutyTerritories
{
    public const uint 欧米茄绝境验证战_时空狭缝 = 1122u;
    public const uint 幻想龙诗绝境战_诗想空间 = 968u;
    public const uint 亚历山大绝境战_差分闭合宇宙 = 887u;
    public const uint 究极神兵绝境战_禁绝幻想 = 777u;
    public const uint 巴哈姆特绝境战_巴哈姆特大迷宫 = 733u;

    public static bool Contains(ushort territoryId) =>
        territoryId == 欧米茄绝境验证战_时空狭缝 ||
        territoryId == 幻想龙诗绝境战_诗想空间 ||
        territoryId == 亚历山大绝境战_差分闭合宇宙 ||
        territoryId == 究极神兵绝境战_禁绝幻想 ||
        territoryId == 巴哈姆特绝境战_巴哈姆特大迷宫;
}
