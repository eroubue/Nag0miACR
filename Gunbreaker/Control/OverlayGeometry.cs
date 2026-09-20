using System;
using System.Numerics;

namespace Nag0mi.Gunbreaker.Control;

internal enum SnapSide
{
    None,
    Left,
    Right,
}

internal sealed record OverlayPlacement
{
    public SnapSide Side { get; set; } = SnapSide.Right;
    public float RelativeX { get; set; } = 1;
    public float RelativeY { get; set; } = 0.5f;
}

internal static class OverlayGeometry
{
    public static Vector2 Resolve(OverlayPlacement placement, Vector2 origin, Vector2 viewportSize, Vector2 barSize)
    {
        var travel = AvailableTravel(viewportSize, barSize);
        var relativeX = placement.Side switch
        {
            SnapSide.Left => 0,
            SnapSide.Right => 1,
            _ => Math.Clamp(placement.RelativeX, 0, 1),
        };

        return origin + new Vector2(relativeX * travel.X, Math.Clamp(placement.RelativeY, 0, 1) * travel.Y);
    }

    public static OverlayPlacement Capture(Vector2 requestedPosition, Vector2 origin, Vector2 viewportSize,
        Vector2 barSize, float scale)
    {
        var travel = AvailableTravel(viewportSize, barSize);
        var offset = Clamp(requestedPosition, origin, viewportSize, barSize) - origin;
        var leftDistance = offset.X;
        var rightDistance = travel.X - offset.X;
        var threshold = 32 * scale;
        var side = SnapSide.None;

        if (MathF.Min(leftDistance, rightDistance) <= threshold)
            side = leftDistance < rightDistance ? SnapSide.Left : SnapSide.Right;

        return new OverlayPlacement
        {
            Side = side,
            RelativeX = side switch
            {
                SnapSide.Left => 0,
                SnapSide.Right => 1,
                _ => travel.X > 0 ? offset.X / travel.X : 0,
            },
            RelativeY = travel.Y > 0 ? offset.Y / travel.Y : 0,
        };
    }

    public static Vector2 Clamp(Vector2 position, Vector2 origin, Vector2 viewportSize, Vector2 size)
    {
        return Vector2.Clamp(position, origin, origin + AvailableTravel(viewportSize, size));
    }

    public static Vector2 SettingsPosition(Vector2 barPosition, Vector2 barSize, SnapSide side, Vector2 origin,
        Vector2 viewportSize, Vector2 settingsSize, float scale)
    {
        var openRight = side switch
        {
            SnapSide.Left => true,
            SnapSide.Right => false,
            _ => origin.X + viewportSize.X - (barPosition.X + barSize.X) >= barPosition.X - origin.X,
        };
        var gap = 8 * scale;
        var x = openRight ? barPosition.X + barSize.X + gap : barPosition.X - settingsSize.X - gap;

        return Clamp(new Vector2(x, barPosition.Y), origin, viewportSize, settingsSize);
    }

    private static Vector2 AvailableTravel(Vector2 viewportSize, Vector2 size)
    {
        // An oversized window cannot fit; keep its top-left corner reachable.
        return Vector2.Max(Vector2.Zero, viewportSize - size);
    }
}
