using System.Numerics;
using Nag0mi.Gunbreaker.Control;

namespace Nag0mi.Tests;

internal static class GeometryTests
{
    public static void Run()
    {
        DefaultPlacementUsesRightEdgeAndVerticalCenter();
        LeftPlacementUsesViewportOrigin();
        FloatingPlacementPreservesRatiosAfterResize();
        CaptureSnapsAtScaledThreshold();
        DraggingAwayFromEdgeReleasesSnap();
        CaptureClampsOutsideViewport();
        CaptureChoosesNearestEdgeWhenSnapZonesOverlap();
        FloatingCaptureRoundTripsWithNegativeOrigin();
        ClampKeepsBarWithinViewport();
        OversizedBarUsesViewportOrigin();
        SettingsOpenInwardWithScaledGap();
        FloatingSettingsUseAvailableSpaceAndClampVertically();
        OversizedSettingsUseViewportOrigin();
    }

    private static void DefaultPlacementUsesRightEdgeAndVerticalCenter()
    {
        var position = OverlayGeometry.Resolve(new OverlayPlacement(), new Vector2(100, 200),
            new Vector2(1000, 800), new Vector2(80, 200));

        Near(new Vector2(1020, 500), position);
    }

    private static void LeftPlacementUsesViewportOrigin()
    {
        var placement = new OverlayPlacement { Side = SnapSide.Left, RelativeX = 0.75f, RelativeY = 0.25f };
        var position = OverlayGeometry.Resolve(placement, new Vector2(-1920, -100),
            new Vector2(1920, 1080), new Vector2(80, 200));

        Near(new Vector2(-1920, 120), position);
    }

    private static void FloatingPlacementPreservesRatiosAfterResize()
    {
        var placement = new OverlayPlacement { Side = SnapSide.None, RelativeX = 0.25f, RelativeY = 0.75f };

        Near(new Vector2(350, 575), OverlayGeometry.Resolve(placement, new Vector2(100, 50),
            new Vector2(1100, 900), new Vector2(100, 200)));
        Near(new Vector2(550, 950), OverlayGeometry.Resolve(placement, new Vector2(100, 50),
            new Vector2(1900, 1400), new Vector2(100, 200)));
    }

    private static void CaptureSnapsAtScaledThreshold()
    {
        var origin = new Vector2(100, 200);
        var viewport = new Vector2(1000, 800);
        var size = new Vector2(80, 200);
        var left = OverlayGeometry.Capture(new Vector2(164, 350), origin, viewport, size, 2);
        var right = OverlayGeometry.Capture(new Vector2(956, 350), origin, viewport, size, 2);

        Check.Equal(SnapSide.Left, left.Side);
        Check.Near(0, left.RelativeX);
        Check.Near(0.25f, left.RelativeY);
        Check.Equal(SnapSide.Right, right.Side);
        Check.Near(1, right.RelativeX);
        Near(new Vector2(1020, 350), OverlayGeometry.Resolve(right, origin, viewport, size));
    }

    private static void DraggingAwayFromEdgeReleasesSnap()
    {
        var origin = new Vector2(100, 200);
        var viewport = new Vector2(1000, 800);
        var size = new Vector2(80, 200);
        var left = OverlayGeometry.Capture(new Vector2(164.25f, 350), origin, viewport, size, 2);
        var right = OverlayGeometry.Capture(new Vector2(955.75f, 350), origin, viewport, size, 2);

        Check.Equal(SnapSide.None, left.Side);
        Check.Equal(SnapSide.None, right.Side);
        Near(new Vector2(164.25f, 350), OverlayGeometry.Resolve(left, origin, viewport, size));
        Near(new Vector2(955.75f, 350), OverlayGeometry.Resolve(right, origin, viewport, size));
    }

    private static void CaptureClampsOutsideViewport()
    {
        var origin = new Vector2(100, 200);
        var viewport = new Vector2(1000, 800);
        var size = new Vector2(80, 200);
        var topLeft = OverlayGeometry.Capture(new Vector2(-1000, -1000), origin, viewport, size, 1);
        var bottomRight = OverlayGeometry.Capture(new Vector2(5000, 5000), origin, viewport, size, 1);

        Check.Equal(SnapSide.Left, topLeft.Side);
        Near(origin, OverlayGeometry.Resolve(topLeft, origin, viewport, size));
        Check.Equal(SnapSide.Right, bottomRight.Side);
        Near(new Vector2(1020, 800), OverlayGeometry.Resolve(bottomRight, origin, viewport, size));
    }

    private static void CaptureChoosesNearestEdgeWhenSnapZonesOverlap()
    {
        var origin = new Vector2(100, 200);
        var viewport = new Vector2(120, 300);
        var size = new Vector2(80, 200);

        Check.Equal(SnapSide.Left,
            OverlayGeometry.Capture(new Vector2(110, 210), origin, viewport, size, 1).Side);
        Check.Equal(SnapSide.Right,
            OverlayGeometry.Capture(new Vector2(130, 210), origin, viewport, size, 1).Side);
    }

    private static void FloatingCaptureRoundTripsWithNegativeOrigin()
    {
        var origin = new Vector2(-1920, -100);
        var viewport = new Vector2(1920, 1080);
        var size = new Vector2(80, 200);
        var requested = new Vector2(-1320, 560);
        var placement = OverlayGeometry.Capture(requested, origin, viewport, size, 1.5f);

        Check.Equal(SnapSide.None, placement.Side);
        Check.Near(600f / 1840f, placement.RelativeX);
        Check.Near(0.75f, placement.RelativeY);
        Near(requested, OverlayGeometry.Resolve(placement, origin, viewport, size));
    }

    private static void ClampKeepsBarWithinViewport()
    {
        var origin = new Vector2(-300, 200);
        var viewport = new Vector2(1000, 800);
        var size = new Vector2(80, 200);

        Near(new Vector2(-300, 800), OverlayGeometry.Clamp(new Vector2(-500, 1500), origin, viewport, size));
        Near(new Vector2(620, 200), OverlayGeometry.Clamp(new Vector2(1500, -500), origin, viewport, size));
        Near(new Vector2(200, 400), OverlayGeometry.Clamp(new Vector2(200, 400), origin, viewport, size));
    }

    private static void OversizedBarUsesViewportOrigin()
    {
        var origin = new Vector2(-50, 100);
        var viewport = new Vector2(40, 60);
        var size = new Vector2(80, 200);
        var placement = OverlayGeometry.Capture(new Vector2(300, 500), origin, viewport, size, 1);

        Near(origin, OverlayGeometry.Clamp(new Vector2(300, 500), origin, viewport, size));
        Near(origin, OverlayGeometry.Resolve(placement, origin, viewport, size));
        Check.True(float.IsFinite(placement.RelativeX));
        Check.True(float.IsFinite(placement.RelativeY));
    }

    private static void SettingsOpenInwardWithScaledGap()
    {
        var origin = new Vector2(100, 200);
        var viewport = new Vector2(1000, 800);
        var barSize = new Vector2(80, 200);
        var settingsSize = new Vector2(300, 400);

        Near(new Vector2(196, 300), OverlayGeometry.SettingsPosition(new Vector2(100, 300), barSize,
            SnapSide.Left, origin, viewport, settingsSize, 2));
        Near(new Vector2(704, 300), OverlayGeometry.SettingsPosition(new Vector2(1020, 300), barSize,
            SnapSide.Right, origin, viewport, settingsSize, 2));
    }

    private static void FloatingSettingsUseAvailableSpaceAndClampVertically()
    {
        var origin = new Vector2(-1000, -200);
        var viewport = new Vector2(1000, 800);
        var barSize = new Vector2(80, 200);
        var settingsSize = new Vector2(300, 400);

        Near(new Vector2(-612, 200), OverlayGeometry.SettingsPosition(new Vector2(-700, 400), barSize,
            SnapSide.None, origin, viewport, settingsSize, 1));
        Near(new Vector2(-608, -200), OverlayGeometry.SettingsPosition(new Vector2(-300, -250), barSize,
            SnapSide.None, origin, viewport, settingsSize, 1));
    }

    private static void OversizedSettingsUseViewportOrigin()
    {
        var origin = new Vector2(-100, 200);
        var position = OverlayGeometry.SettingsPosition(new Vector2(-100, 200), new Vector2(80, 200),
            SnapSide.Left, origin, new Vector2(100, 300), new Vector2(300, 400), 1);

        Near(origin, position);
    }

    private static void Near(Vector2 expected, Vector2 actual)
    {
        Check.Near(expected.X, actual.X);
        Check.Near(expected.Y, actual.Y);
    }
}
