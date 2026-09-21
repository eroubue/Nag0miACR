namespace Nag0mi.Tests;

internal static class Program
{
    private static int Main()
    {
        try
        {
            ProfileTests.Run();
            AoeBreakpointTests.Run();
            OpenerSelectionTests.Run();
            GeometryTests.Run();
            Console.WriteLine("PASS: snapping, drag release, normalized resize, clamping and inward settings placement");
            WindowLeaseTests.Run();
            SwapOrderTests.Run();
            OrderMergeTests.Run();
            CustomHotkeyTargetTests.Run();
            QtIconTests.Run();
            ModeButtonColorTests.Run();
            ShuimoPaletteTests.Run();
            BerthShapeTests.Run();
            Console.WriteLine($"PASS: {Check.Assertions} assertions");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}

internal static class Check
{
    public static int Assertions { get; private set; }
    public static void True(bool value, string? message = null)
    {
        Assertions++;
        if (!value) throw new Exception(message ?? "Expected true");
    }
    public static void Equal<T>(T expected, T actual)
        => True(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected {expected}, got {actual}");
    public static void Near(float expected, float actual, float epsilon = .001f)
        => True(Math.Abs(expected - actual) <= epsilon, $"Expected {expected}, got {actual}");
    public static void Throws<T>(Action action) where T : Exception
    {
        Assertions++;
        try { action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}");
    }
}
