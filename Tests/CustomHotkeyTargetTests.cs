using Nag0mi.Common.Data;

namespace Nag0mi.Tests;

internal static class CustomHotkeyTargetTests
{
    public static void Run()
    {
        // 标签与枚举同序同数
        var values = Enum.GetValues<CustomHotkeyTarget>();
        Check.Equal(values.Length, CustomHotkeyTargets.Labels.Length);
        Check.Equal("自己", CustomHotkeyTargets.Label(CustomHotkeyTarget.Self));
        Check.Equal("血量最低的输出职业", CustomHotkeyTargets.Label(CustomHotkeyTarget.LowestHpDps));
        Check.Equal("鼠标目标", CustomHotkeyTargets.Label(CustomHotkeyTarget.MouseOver));
        Check.Equal("小队成员8", CustomHotkeyTargets.Label(CustomHotkeyTarget.Party8));

        // 宿主 ActionTargetType 底层值映射（Self=0, Target=1, TargetOfTarget=2,
        // MouseOver=4, LowestHealthPartyMember=5, PartyMember2..8=6..12）
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.Self) == 0);
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.Target) == 1);
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.TargetOfTarget) == 2);
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.MouseOver) == 4);
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.LowestHpParty) == 5);
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.Party2) == 6);
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.Party5) == 9);
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.Party8) == 12);
        // 需自解析的四种返回 null
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.LowestHpTank) == null);
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.LowestHpHealer) == null);
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.LowestHpDps) == null);
        Check.True(CustomHotkeyTargets.ToNativeTargetType(CustomHotkeyTarget.DeadParty) == null);

        // 命名去重：无冲突原样返回, 重名追加 ·2/·3…
        Check.Equal("极光·目标", CustomHotkeyTargets.GenerateUniqueName("极光·目标", new[] { "挑衅" }));
        Check.Equal("极光·目标·2", CustomHotkeyTargets.GenerateUniqueName("极光·目标", new[] { "极光·目标" }));
        Check.Equal("极光·目标·3",
            CustomHotkeyTargets.GenerateUniqueName("极光·目标", new[] { "极光·目标", "极光·目标·2" }));
        Console.WriteLine("PASS: custom hotkey target labels, native mapping, unique naming");
    }
}
