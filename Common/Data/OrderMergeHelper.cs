// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
namespace Nag0mi.Common.Data;

// QT/热键排序的纯逻辑：存储顺序与定义顺序的合并、按可见集合的移动（插入语义）。
// 不依赖宿主单例，可单测；Nag0miUISettings 的排序方法委托到这里。
internal static class OrderMergeHelper
{
    // 合并顺序：storedOrder 中仍存在于全集的键按其顺序在前（去重），
    // 全集其余键按定义顺序补尾。storedOrder 为空时即全集定义顺序。
    public static List<string> MergeOrder(IEnumerable<string> storedOrder, IEnumerable<string> allKeys)
    {
        var all = allKeys as ICollection<string> ?? allKeys.ToList();
        var valid = new HashSet<string>(all);
        var result = new List<string>(all.Count);
        var seen = new HashSet<string>();
        foreach (var k in storedOrder)
            if (valid.Contains(k) && seen.Add(k)) result.Add(k);
        foreach (var k in all)
            if (seen.Add(k)) result.Add(k);
        return result;
    }

    // 可见槽位移动（插入语义）：key 在可见序列中移到 可见目标索引，隐藏键保持原槽位。
    // 返回重构后的全序；key 不可见或已在目标位置时返回 null（调用方无需落盘）。
    public static List<string>? MoveWithinVisible(IReadOnlyList<string> full, ISet<string> visibleSet,
        string key, int 可见目标索引)
    {
        if (!visibleSet.Contains(key)) return null;
        var visible = full.Where(visibleSet.Contains).ToList();
        var from = visible.IndexOf(key);
        if (from < 0) return null;   // key 不在全序的可见序列里（可见集合与全序脱节）
        可见目标索引 = Math.Clamp(可见目标索引, 0, visible.Count - 1);
        if (from == 可见目标索引) return null;
        var moved = visible[from];
        visible.RemoveAt(from);
        visible.Insert(可见目标索引, moved);
        // 重构全序: 可见槽位按新序填入, 隐藏键保持原槽位
        var result = new List<string>(full.Count);
        var vi = 0;
        foreach (var k in full)
            result.Add(visibleSet.Contains(k) ? visible[vi++] : k);
        return result;
    }
}
