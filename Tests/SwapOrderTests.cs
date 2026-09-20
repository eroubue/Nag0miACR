using System.Numerics;
using Nag0mi.Common.UI;

namespace Nag0mi.Tests;

internal static class SwapOrderTests
{
    // 测试网格：原点 (100,50)，3 列 5 格，格边长 34，间距 10（节距 44）。
    private static readonly Vector2 Origin = new(100f, 50f);
    private const int Columns = 3;
    private const int Count = 5;
    private const float Tile = 34f;
    private const float Spacing = 10f;
    private const float Pitch = Tile + Spacing;

    public static void Run()
    {
        SlotIndexMapsMouseToGridSlot();
        SlotIndexRoundsSpacingGapToNearestSlot();
        SlotIndexRejectsOutsideGrid();
        SlotIndexRejectsDegenerateGridAndTrailingSlots();
        SwapExchangesTwoSlots();
        SwapSelfAndOutOfRangeAreNoOps();
        AnimatePositionConvergesAndSnaps();
        AnimatePositionNeverOvershoots();
        AnimateValueConvergesAndSnaps();
        Console.WriteLine("PASS: slot index mapping, swap semantics, animation convergence");
    }

    private static int Slot(Vector2 mouse)
        => SwapOrderHelper.SlotIndexFromPosition(mouse, Origin, Columns, Count, Tile, Spacing);

    private static void SlotIndexMapsMouseToGridSlot()
    {
        Check.Equal(0, Slot(Origin));                       // 首格左上角
        Check.Equal(0, Slot(Origin + new Vector2(17, 17))); // 首格中心
        Check.Equal(1, Slot(Origin + new Vector2(Pitch + 17, 17)));
        Check.Equal(3, Slot(Origin + new Vector2(17, Pitch + 17))); // 第二行首格
        Check.Equal(4, Slot(Origin + new Vector2(Pitch + 17, Pitch + 17)));
    }

    private static void SlotIndexRoundsSpacingGapToNearestSlot()
    {
        // 槽位 0 占 lx∈[0,34)，槽位 1 从 lx=44 起；间距带 [34,44) 以 (lx+5)/44 四舍五入，
        // 分界在 lx=39：左半归左槽，右半归右槽。
        Check.Equal(0, Slot(Origin + new Vector2(38, 17)));
        Check.Equal(1, Slot(Origin + new Vector2(39, 17)));
        // 负方向同理：半个间距宽容度内仍归首格，越过则视为网格外。
        Check.Equal(0, Slot(Origin + new Vector2(-4, 17)));
        Check.Equal(0, Slot(Origin + new Vector2(17, -4)));
    }

    private static void SlotIndexRejectsOutsideGrid()
    {
        Check.Equal(-1, Slot(Origin + new Vector2(-6, 17)));   // 越过左界半个间距
        Check.Equal(-1, Slot(Origin + new Vector2(17, -6)));   // 越过上界
        Check.Equal(-1, Slot(Origin + new Vector2(127, 17)));  // 第 4 列起点，超出 3 列
        Check.Equal(-1, Slot(Origin + new Vector2(17, 83)));   // 第 3 行起点，超出 2 行
    }

    private static void SlotIndexRejectsDegenerateGridAndTrailingSlots()
    {
        Check.Equal(-1, SwapOrderHelper.SlotIndexFromPosition(Origin, Origin, 0, Count, Tile, Spacing));
        Check.Equal(-1, SwapOrderHelper.SlotIndexFromPosition(Origin, Origin, Columns, 0, Tile, Spacing));
        Check.Equal(-1, SwapOrderHelper.SlotIndexFromPosition(Origin, Origin, -1, -1, Tile, Spacing));
        // 末行空缺格（5 格 3 列 → 第 2 行第 3 列下标 5 不存在）。
        Check.Equal(-1, Slot(Origin + new Vector2(Pitch * 2 + 17, Pitch + 17)));
    }

    private static void SwapExchangesTwoSlots()
    {
        var order = new List<string> { "a", "b", "c" };
        SwapOrderHelper.Swap(order, 0, 2);
        Check.True(order.SequenceEqual(new[] { "c", "b", "a" }));
    }

    private static void SwapSelfAndOutOfRangeAreNoOps()
    {
        foreach (var (a, b) in new[] { (1, 1), (-1, 1), (0, 3), (3, 0), (2, -1), (int.MinValue, 0) })
        {
            var order = new List<string> { "a", "b", "c" };
            SwapOrderHelper.Swap(order, a, b);
            Check.True(order.SequenceEqual(new[] { "a", "b", "c" }), $"Swap({a},{b}) 应为 no-op");
        }
    }

    private static void AnimatePositionConvergesAndSnaps()
    {
        var target = new Vector2(100, 50);
        var current = Vector2.Zero;
        var previous = Vector2.Distance(current, target);
        for (var i = 0; i < 600 && current != target; i++)
        {
            var next = SwapOrderHelper.AnimatePosition(current, target, 1f / 60f);
            var distance = Vector2.Distance(next, target);
            Check.True(distance < previous || next == target, "每帧应严格逼近目标");
            previous = distance;
            current = next;
        }
        Check.Equal(target, current); // 距离小于 0.1px 时直接吸附
        Check.Equal(target, SwapOrderHelper.AnimatePosition(target, target, 1f / 60f));
    }

    private static void AnimatePositionNeverOvershoots()
    {
        var target = new Vector2(100, 50);
        // deltaTime 钳制在 0.1s：掉帧一大截也只走指数步，不会越过目标。
        var next = SwapOrderHelper.AnimatePosition(Vector2.Zero, target, 10f);
        Check.True(next.X > 0 && next.X < 100 && next.Y > 0 && next.Y < 50);
        // 速度下界 0.01：speed=0 时几乎不动但仍朝目标。
        var slow = SwapOrderHelper.AnimatePosition(Vector2.Zero, target, 1f / 60f, 0f);
        Check.True(slow.X > 0 && slow.X < 1 && slow.Y > 0 && slow.Y < 0.5f);
    }

    private static void AnimateValueConvergesAndSnaps()
    {
        var current = 0f;
        for (var i = 0; i < 600 && current != 100f; i++)
        {
            var next = SwapOrderHelper.AnimateValue(current, 100f, 1f / 60f);
            Check.True(next > current && next <= 100f, "标量版应单调逼近且不越过");
            current = next;
        }
        Check.Equal(100f, current);
        Check.Equal(5f, SwapOrderHelper.AnimateValue(5f, 5f, 1f / 60f));
    }
}
