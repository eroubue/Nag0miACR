// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;

namespace Nag0mi.Common.UI;

// 面板拖拽实时交换排序的纯逻辑：不依赖 ImGui/Dalamud，可单测。
// QT 面板与热键面板共用。
public static class SwapOrderHelper
{
    // 槽位交换动画的默认指数阻尼速度（每秒）。
    public const float DefaultAnimationSpeed = 18f;

    // 鼠标位置 → 网格槽位下标：gridOrigin 为首个槽位左上角，格子按 tileSize+spacing 节距排列。
    // 鼠标落在网格有效范围外（含间距右/下溢出与负方向）返回 -1。
    public static int SlotIndexFromPosition(Vector2 mousePos, Vector2 gridOrigin, int columns, int count, float tileSize, float spacing)
    {
        if (columns <= 0 || count <= 0) return -1;
        var pitchX = tileSize + spacing;
        var pitchY = tileSize + spacing;
        var lx = mousePos.X - gridOrigin.X;
        var ly = mousePos.Y - gridOrigin.Y;
        if (lx < -spacing * 0.5f || ly < -spacing * 0.5f) return -1;
        var col = (int)((lx + spacing * 0.5f) / pitchX);
        var row = (int)((ly + spacing * 0.5f) / pitchY);
        var rows = (count + columns - 1) / columns;
        if (col < 0 || col >= columns || row < 0 || row >= rows) return -1;
        var index = row * columns + col;
        return index < count ? index : -1;
    }

    // 交换语义：顺序表 a、b 两槽位的项互换。自交换或下标越界为 no-op
    // （越界对应拖拽指针落在网格外，SlotIndexFromPosition 返回 -1 的场景）。
    public static void Swap<T>(IList<T> order, int a, int b)
    {
        if (a == b) return;
        if ((uint)a >= (uint)order.Count || (uint)b >= (uint)order.Count) return;
        (order[a], order[b]) = (order[b], order[a]);
    }

    // 位置指数阻尼收敛：返回逼近 target 的下一帧位置，距离小于 0.1px 时直接吸附。
    public static Vector2 AnimatePosition(Vector2 current, Vector2 target, float deltaTime, float speed = DefaultAnimationSpeed)
    {
        var step = 1f - MathF.Exp(-MathF.Max(0.01f, speed) * Math.Clamp(deltaTime, 0f, 0.1f));
        var next = current + (target - current) * step;
        return Vector2.DistanceSquared(next, target) < 0.01f ? target : next;
    }

    // 标量版同一条指数阻尼曲线。
    public static float AnimateValue(float current, float target, float deltaTime, float speed = DefaultAnimationSpeed)
    {
        var step = 1f - MathF.Exp(-MathF.Max(0.01f, speed) * Math.Clamp(deltaTime, 0f, 0.1f));
        var next = current + (target - current) * step;
        return MathF.Abs(next - target) < 0.01f ? target : next;
    }
}
