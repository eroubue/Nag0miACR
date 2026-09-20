using System.Numerics;
using Nag0mi.Common.UI;

namespace Nag0mi.Tests;

internal static class BerthShapeTests
{
    private const float W = OverlayBerthShape.Width;
    private const float H = OverlayBerthShape.DockedHeight;

    public static void Run()
    {
        FloatingOutlineIsCenteredCapsule();
        DockedOutlineSpansFullHeight();
        DockedFilletTangents();
        OutlineStaysStarShaped();
        ButtonsStayInsideOutline();
        OutlineClampsBeyondRange();
        SpringConvergesWithBoundedOvershoot();
        SpringReturnsWithoutDiverging();
        SpringIgnoresZeroDelta();
    }

    private static List<Vector2> Outline(float t, bool mirror = false)
        => OverlayBerthShape.BuildOutline(t, mirror, 1f, Vector2.Zero);

    // t=0 悬浮：贴边点 y 范围恰为 [56,198]（56×198 胶囊在 254 窗内居中）
    private static void FloatingOutlineIsCenteredCapsule()
    {
        var ys = Outline(0).Where(p => Math.Abs(p.X) < 1e-4f).Select(p => p.Y).ToList();
        Check.Near(56, ys.Min());
        Check.Near(198, ys.Max());
    }

    // t=1 吸附：贴边侧全高平直（x=0 覆盖 [0,254]）
    private static void DockedOutlineSpansFullHeight()
    {
        var ys = Outline(1).Where(p => Math.Abs(p.X) < 1e-4f).Select(p => p.Y).ToList();
        Check.Near(0, ys.Min());
        Check.Near(H, ys.Max());
    }

    // t=1 端头凹弧：离边处切线竖直、汇入本体处切线水平
    private static void DockedFilletTangents()
    {
        var pts = Outline(1);
        var k = pts.FindIndex(p => p is { X: 0, Y: 0 });
        Check.True(k > 0, "未找到上贴边接触点 K");
        var leave = pts[k + 1] - pts[k];
        Check.True(Math.Abs(leave.X) < Math.Abs(leave.Y) * .2f, "凹弧应竖直离开贴边");
        var j = pts.FindIndex(p => p is { X: W / 2, Y: 28 });
        Check.True(j > k, "未找到凹弧汇入点 J");
        var join = pts[j] - pts[j - 1];
        Check.True(Math.Abs(join.Y) < Math.Abs(join.X) * .2f, "凹弧应水平汇入本体");
    }

    // 轮廓对起点（贴边中点）星形可见：扇形三角绕向符号一致 → PathFillConvex 填充精确
    private static void OutlineStaysStarShaped()
    {
        foreach (var mirror in new[] { false, true })
            for (var i = 0; i <= 20; i++)
            {
                var t = i / 20f;
                var pts = Outline(t, mirror);
                var v0 = pts[0];
                var sign = 0;
                for (var j = 1; j < pts.Count; j++)
                {
                    var a = pts[j] - v0;
                    var b = pts[(j + 1) % pts.Count] - v0;
                    var cross = a.X * b.Y - a.Y * b.X;
                    if (Math.Abs(cross) < 1e-3f) continue;
                    var s = Math.Sign(cross);
                    if (sign == 0) sign = s;
                    Check.True(sign == s, $"t={t} mirror={mirror} 轮廓对起点非星形可见，凸填充会错乱");
                }
            }
    }

    // 全形变过程四键始终完整落在轮廓内（按钮不会悬空到融边指的透明区）
    private static void ButtonsStayInsideOutline()
    {
        foreach (var mirror in new[] { false, true })
            for (var i = 0; i <= 10; i++)
            {
                var t = i / 10f;
                var pts = Outline(t, mirror);
                foreach (var y in OverlayBerthShape.ButtonRows)
                foreach (var x in new[] { 10.5f, W / 2, 45.5f })
                foreach (var dy in new[] { .5f, 18f, 35.5f })
                    Check.True(Inside(pts, new Vector2(x, y + dy)),
                        $"t={t} mirror={mirror} 按钮点 ({x},{y + dy}) 跑出轮廓");
            }
    }

    // t 越界按 [0,1] 钳制（弹簧过冲不会改变形态）；全网格无 NaN
    private static void OutlineClampsBeyondRange()
    {
        Check.Equal(Outline(1).Count, Outline(1.1f).Count);
        Check.Equal(Outline(1)[3], Outline(1.1f)[3]);
        Check.Equal(Outline(0)[3], Outline(-.1f)[3]);
        for (var i = 0; i <= 20; i++)
            Check.True(Outline(i / 20f).All(p => float.IsFinite(p.X) && float.IsFinite(p.Y)), "轮廓含非有限值");
    }

    // 弹簧：0→1 在 1.5s 内收敛，带轻微过冲（欠阻尼手感）但不过头
    private static void SpringConvergesWithBoundedOvershoot()
    {
        var s = new BerthSpring(0);
        var peak = 0f;
        for (var i = 0; i < 90; i++) // 1.5s @60fps
        {
            s.Step(1, 1f / 60f);
            peak = Math.Max(peak, s.Value);
        }
        Check.Near(1, s.Value, .01f);
        Check.True(peak > 1f, "弹簧应有轻微过冲（欠阻尼手感）");
        Check.True(peak < 1.02f, $"弹簧过冲过大: {peak}");
    }

    // 形变中途换向：从半路返回不发散、不越界
    private static void SpringReturnsWithoutDiverging()
    {
        var s = new BerthSpring(0);
        for (var i = 0; i < 6; i++) s.Step(1, 1f / 60f);
        for (var i = 0; i < 90; i++)
        {
            s.Step(0, 1f / 60f);
            Check.True(s.Value is > -.1f and < 1.1f, $"换向后弹簧越界: {s.Value}");
        }
        Check.Near(0, s.Value, .01f);
    }

    private static void SpringIgnoresZeroDelta()
    {
        var s = new BerthSpring(.4f);
        s.Step(1, 0);
        Check.Near(.4f, s.Value);
        Check.Near(0, s.Velocity);
    }

    private static bool Inside(List<Vector2> pts, Vector2 p)
    {
        var inside = false;
        for (var i = 0; i < pts.Count; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Count];
            if (a.Y > p.Y == b.Y > p.Y) continue;
            if (p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }
}
