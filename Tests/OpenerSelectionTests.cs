using Nag0mi.Gunbreaker.Data;

namespace Nag0mi.Tests;

internal static class OpenerSelectionTests
{
    public static void Run()
    {
        // 枚举顺序 = 设置下拉框顺序 = 存档持久化值，禁止重排或中间插入。
        var names = Enum.GetNames<GunbreakerSettings.起手选择枚举>();
        Check.True(names.SequenceEqual(new[]
        {
            "妖星起手", "无情2g起手", "绝欧起手", "龙诗起手", "绝亚起手", "神兵起手", "巴哈起手"
        }), "起手选择枚举顺序: " + string.Join(",", names));
        Console.WriteLine("PASS: 起手选择枚举顺序固定为7项");

        var root = FindRepoRoot();
        var openerDir = Path.Combine(root, "Gunbreaker", "Opener");

        // Lv70 起手（神兵/巴哈）禁止出现 70 级未学技能，否则 RequiresVerification 永远卡死。
        foreach (var file in new[] { "神兵.cs", "巴哈.cs" })
        {
            var text = File.ReadAllText(Path.Combine(openerDir, file));
            foreach (var banned in new[] { "血壤", "命运之环", "倍攻", "超音速", "爆破领域" })
                Check.True(!text.Contains($"GunbreakerSkill.{banned}"), $"{file} 含 70 级不可用技能 {banned}");
        }

        // Lv80 起手（绝亚）禁止 86+/90 级技能。
        var tea = File.ReadAllText(Path.Combine(openerDir, "绝亚.cs"));
        foreach (var banned in new[] { "倍攻", "超音速" })
            Check.True(!tea.Contains($"GunbreakerSkill.{banned}"), $"绝亚.cs 含 80 级不可用技能 {banned}");

        // 7 个起手全部注册进 Openers 字典。
        var rotation = File.ReadAllText(Path.Combine(root, "Gunbreaker", "GunbreakerRotation.cs"));
        foreach (var name in names)
            Check.True(rotation.Contains($"[\"{name}\"]"), $"Openers 未注册 {name}");
        Console.WriteLine("PASS: 绝本起手等级合法性与注册完整性");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Nag0mi.csproj")))
            dir = dir.Parent;
        Check.True(dir != null, "未找到仓库根目录(Nag0mi.csproj)");
        return dir!.FullName;
    }
}
