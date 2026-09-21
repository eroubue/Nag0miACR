using Nag0mi.Common.UI;

namespace Nag0mi.Tests;

internal static class ModeButtonColorTests
{
    public static void Run()
    {
        MapsHighEndToRed();
        MapsDailyToCyan();
        MapsCustomToIndigo();
        UnknownNamesFallBackToMain();
    }

    private static void MapsHighEndToRed()
    {
        Check.Equal(ShuimoPalette.Hex(0x861717), SimplePalette.ModeButtonColor("高难"));
    }

    private static void MapsDailyToCyan()
    {
        Check.Equal(ShuimoPalette.Hex(0x4A9992), SimplePalette.ModeButtonColor("日随"));
    }

    private static void MapsCustomToIndigo()
    {
        Check.Equal(ShuimoPalette.Hex(0x1661AB), SimplePalette.ModeButtonColor("自定义"));
    }

    private static void UnknownNamesFallBackToMain()
    {
        Check.Equal(ShuimoPalette.Main, SimplePalette.ModeButtonColor("模式7"));
        Check.Equal(ShuimoPalette.Main, SimplePalette.ModeButtonColor(null));
    }
}
