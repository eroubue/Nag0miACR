using Nag0mi.Common.Data;

namespace Nag0mi.Tests;

internal static class OrderMergeTests
{
    private static readonly string[] All = { "无情", "爆发", "子弹连", "血壤" };

    public static void Run()
    {
        EmptyStoredOrderYieldsDefinitionOrder();
        StoredOrderFirstThenNewKeysAppended();
        StaleAndDuplicateStoredKeysDropped();
        MoveWithinVisibleReordersVisibleSlots();
        MoveWithinVisibleKeepsHiddenSlotsInPlace();
        MoveWithinVisibleClampsTargetIndex();
        MoveWithinVisibleRejectsHiddenOrUnknownKeyAndNoOp();
        Console.WriteLine("PASS: order merge and visible-slot move semantics");
    }

    private static void EmptyStoredOrderYieldsDefinitionOrder()
    {
        Check.True(OrderMergeHelper.MergeOrder([], All).SequenceEqual(All));
    }

    private static void StoredOrderFirstThenNewKeysAppended()
    {
        // 存过的键按其顺序在前，定义表新增键（子弹连/血壤）按定义顺序补尾。
        var merged = OrderMergeHelper.MergeOrder(new[] { "爆发", "无情" }, All);
        Check.True(merged.SequenceEqual(new[] { "爆发", "无情", "子弹连", "血壤" }));
    }

    private static void StaleAndDuplicateStoredKeysDropped()
    {
        // 已不在定义表的死键剔除，存储表里的重复项去重（首次出现的位置生效）。
        var merged = OrderMergeHelper.MergeOrder(new[] { "旧键", "爆发", "爆发", "废键" }, All);
        Check.True(merged.SequenceEqual(new[] { "爆发", "无情", "子弹连", "血壤" }));
    }

    private static void MoveWithinVisibleReordersVisibleSlots()
    {
        var all = new HashSet<string>(All);
        var moved = OrderMergeHelper.MoveWithinVisible(All, all, "无情", 2);
        Check.True(moved != null && moved.SequenceEqual(new[] { "爆发", "子弹连", "无情", "血壤" }));
        moved = OrderMergeHelper.MoveWithinVisible(All, all, "血壤", 1);
        Check.True(moved != null && moved.SequenceEqual(new[] { "无情", "血壤", "爆发", "子弹连" }));
    }

    private static void MoveWithinVisibleKeepsHiddenSlotsInPlace()
    {
        // 隐藏键不占格子也不参与重排：可见序列 [无情,子弹连,血壤] 中把 子弹连 移到 0，
        // 隐藏键 爆发 保持原槽位 1。
        var visible = new HashSet<string> { "无情", "子弹连", "血壤" };
        var moved = OrderMergeHelper.MoveWithinVisible(All, visible, "子弹连", 0);
        Check.True(moved != null && moved.SequenceEqual(new[] { "子弹连", "爆发", "无情", "血壤" }));
    }

    private static void MoveWithinVisibleClampsTargetIndex()
    {
        var all = new HashSet<string>(All);
        var moved = OrderMergeHelper.MoveWithinVisible(All, all, "无情", 99);
        Check.True(moved != null && moved.SequenceEqual(new[] { "爆发", "子弹连", "血壤", "无情" }));
        moved = OrderMergeHelper.MoveWithinVisible(All, all, "血壤", -5);
        Check.True(moved != null && moved.SequenceEqual(new[] { "血壤", "无情", "爆发", "子弹连" }));
    }

    private static void MoveWithinVisibleRejectsHiddenOrUnknownKeyAndNoOp()
    {
        var all = new HashSet<string>(All);
        // 已在目标位置 → null（调用方不落盘）。
        Check.Equal(null, OrderMergeHelper.MoveWithinVisible(All, all, "无情", 0));
        // 隐藏键不可见 → null。
        var visible = new HashSet<string> { "无情", "子弹连", "血壤" };
        Check.Equal(null, OrderMergeHelper.MoveWithinVisible(All, visible, "爆发", 0));
        // 可见集合与全序脱节（键不在全序里）→ null，不重构出坏表。
        var strays = new HashSet<string> { "不存在" };
        Check.Equal(null, OrderMergeHelper.MoveWithinVisible(All, strays, "不存在", 0));
    }
}
