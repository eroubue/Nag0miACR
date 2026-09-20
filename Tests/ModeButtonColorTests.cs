using Nag0mi.Common.UI;

namespace Nag0mi.Tests;

internal static class ModeButtonColorTests
{
    public static void Run()
    {
        MapsHighEndToRed();
        MapsDailyToGreen();
        MapsCustomToYellow();
        UnknownNamesFallBackToPrimary();
    }

    private static void MapsHighEndToRed()
    {
        Check.Equal(SimplePalette.StateOff, SimplePalette.ModeButtonColor("高难"));
    }

    private static void MapsDailyToGreen()
    {
        Check.Equal(SimplePalette.StateRunning, SimplePalette.ModeButtonColor("日随"));
    }

    private static void MapsCustomToYellow()
    {
        Check.Equal(SimplePalette.ModeCustom, SimplePalette.ModeButtonColor("自定义"));
    }

    private static void UnknownNamesFallBackToPrimary()
    {
        Check.Equal(SimplePalette.PrimaryHover, SimplePalette.ModeButtonColor("模式7"));
        Check.Equal(SimplePalette.PrimaryHover, SimplePalette.ModeButtonColor(null));
    }
}
