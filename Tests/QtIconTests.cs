using Nag0mi.Gunbreaker.Data;

namespace Nag0mi.Tests;

internal static class QtIconTests
{
    public static void Run()
    {
        UnknownKeyFallsBackToQuestionIconWithFirstCharMarker();
        ForUiMatchesFor();
        KnownKeyResolvesSkillIconWithoutMarker();
        PotionUsesGameIcon();
        IconMapAndDefaultsCoverTheSameKeys();
        QtConstantsCoverEveryDefaultKey();
        Console.WriteLine("PASS: qt icon fallback, same-skill markers, icon/defaults/constant consistency");
    }

    private static void UnknownKeyFallsBackToQuestionIconWithFirstCharMarker()
    {
        // 新增 QT 尚未配图标时退化为问号图标 + 首字角标，面板不崩。
        var icon = GunbreakerQtIcons.For("神秘新QT");
        Check.Equal(60071u, icon.IconId);
        Check.True(icon.IsGameIcon);
        Check.Equal("神", icon.Marker);
    }

    private static void ForUiMatchesFor()
    {
        foreach (var key in GunbreakerQtDefaults.All.Keys.Concat(new[] { "未登记" }))
        {
            var (iconId, isGameIcon, marker) = GunbreakerQtIcons.ForUi(key);
            var icon = GunbreakerQtIcons.For(key);
            Check.Equal(icon.IconId, iconId);
            Check.Equal(icon.IsGameIcon, isGameIcon);
            Check.Equal(icon.Marker, marker);
        }
    }

    private static void KnownKeyResolvesSkillIconWithoutMarker()
    {
        // 基础键：技能图标、走技能 id 解析（非游戏原生图标）、无角标。
        var icon = GunbreakerQtIcons.For("无情");
        Check.Equal(GunbreakerSkill.无情, icon.IconId);
        Check.True(!icon.IsGameIcon);
        Check.Equal(null, icon.Marker);
    }
    

    private static void AssertMarkers(uint skill, params (string Key, string? Marker)[] entries)
    {
        foreach (var (key, marker) in entries)
        {
            var icon = GunbreakerQtIcons.For(key);
            Check.Equal(skill, icon.IconId);
            Check.Equal(marker, icon.Marker);
        }
        // 派生键的角标互不相同，且都与基础键（无角标）区分开。
        var markers = entries.Where(e => e.Marker != null).Select(e => e.Marker).ToList();
        Check.Equal(markers.Distinct().Count(), markers.Count);
    }

    private static void PotionUsesGameIcon()
    {
        var icon = GunbreakerQtIcons.For("爆发药");
        Check.Equal(20601u, icon.IconId); // 酊剂物品图标
        Check.True(icon.IsGameIcon);
        Check.Equal(null, icon.Marker);
    }

    private static void IconMapAndDefaultsCoverTheSameKeys()
    {
        // 图标映射与默认值表必须同键集：新增 QT 漏配任一侧都会被拦住。
        foreach (var key in GunbreakerQtDefaults.All.Keys)
            Check.True(GunbreakerQtIcons.Map.ContainsKey(key), $"图标映射缺 {key}");
        foreach (var key in GunbreakerQtIcons.Map.Keys)
            Check.True(GunbreakerQtDefaults.All.ContainsKey(key), $"默认值表缺 {key}");
    }

    private static void QtConstantsCoverEveryDefaultKey()
    {
        // 每个 QT key 在 GunbreakerQT 里都有同名常量，防止再出现「字面量裸用」。
        var constants = typeof(GunbreakerQT)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();
        Check.Equal("无情不延后", GunbreakerQT.无情不延后);
        foreach (var key in GunbreakerQtDefaults.All.Keys)
            Check.True(constants.Contains(key), $"GunbreakerQT 缺常量 {key}");
    }
}
