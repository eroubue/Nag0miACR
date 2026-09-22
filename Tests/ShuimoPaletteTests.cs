using Nag0mi.Common.UI;

namespace Nag0mi.Tests;

// 水墨色板明/暗切换的纯逻辑单测：直接翻转 ShuimoPalette.DarkMode 静态开关验证取色。
internal static class ShuimoPaletteTests
{
    public static void Run()
    {
        try
        {
            LightModeColors();
            DarkModeColors();
            ModeIndependentColors();
        }
        finally
        {
            ShuimoPalette.DarkMode = false;   // 复位, 不影响其它用例
        }
    }

    private static void LightModeColors()
    {
        ShuimoPalette.DarkMode = false;
        Check.Equal(ShuimoPalette.Hex(0x2B333E), ShuimoPalette.Text);        // 墨
        Check.Equal(ShuimoPalette.Hex(0x000000), ShuimoPalette.TextSecondary); // 黑（灰字低对比, 统一改黑）
        Check.Equal(ShuimoPalette.Hex(0x000000), ShuimoPalette.TextDisabled);
    }

    private static void DarkModeColors()
    {
        ShuimoPalette.DarkMode = true;
        Check.Equal(ShuimoPalette.Hex(0xEEEEEE), ShuimoPalette.Text);        // 缟羽
        Check.Equal(ShuimoPalette.Hex(0x9AA7B1), ShuimoPalette.TextSecondary); // 青鸾
        Check.Equal(ShuimoPalette.Hex(0x5E616D), ShuimoPalette.TextDisabled);
    }

    private static void ModeIndependentColors()
    {
        // 明暗同值的语义色
        foreach (var dark in new[] { false, true })
        {
            ShuimoPalette.DarkMode = dark;
            Check.Equal(ShuimoPalette.Hex(0x861717), ShuimoPalette.Main);     // 血红
            Check.Equal(ShuimoPalette.Hex(0x1661AB), ShuimoPalette.Hover);    // 靛蓝
            Check.Equal(ShuimoPalette.Hex(0x4A9992), ShuimoPalette.Running);  // 青
            Check.Equal(ShuimoPalette.Hex(0xE8B004), ShuimoPalette.Hold);     // 藤黄
            Check.Equal(ShuimoPalette.Hex(0x5E616D), ShuimoPalette.BorderBase);
            Check.Equal(ShuimoPalette.Hex(0x31322C), ShuimoPalette.InkStick); // 京元
        }
    }
}
