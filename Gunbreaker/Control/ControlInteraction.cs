namespace Nag0mi.Gunbreaker.Control;

// Commands derived from the host on each click; no execution state is stored here.
internal enum ControlCommand { Resume, Pause, Close }

internal static class ControlInteraction
{
    public static bool IsPaused(bool disabled, bool held)
        => !disabled && held;

    public static ControlCommand Click(bool disabled, bool held, bool rightClick)
        => rightClick ? ControlCommand.Close
            : disabled || held ? ControlCommand.Resume : ControlCommand.Pause;
}
