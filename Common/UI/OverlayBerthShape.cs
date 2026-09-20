using System.Numerics;

namespace Nag0mi.Common.UI;

// 控制条吸附融边的轮廓生成与弹簧驱动（纯逻辑，无 ImGui 依赖，供逻辑测试直接编译）。
// 形态族 t∈[0,1]：t=0 居中胶囊（可见区 56×198）↔ t=1 贴边侧全高平直、上下端 S 曲线凹弧融入屏边。
// 端头凹弧参考上游实现：单条三次贝塞尔，控制点按 k=0.55 圆弧近似——离边竖直切出、汇入本体水平。
internal static class OverlayBerthShape
{
    // 逻辑尺寸（100% 缩放）：条宽 56；悬浮可见高 198；窗口固定高 254（上下各 28 预留给融边爬升）
    public const float Width = 56f;
    public const float FloatingHeight = 198f;
    public const float DockedHeight = 254f;

    // 四键槽位 Y（固定高窗内）：等价原 198 窗内的 12/58/104/150
    public static readonly float[] ButtonRows = { 40f, 86f, 132f, 178f };

    private const float HandleK = .55f;      // 贝塞尔手柄的圆弧近似系数
    private const int Segments = 14;         // 每条贝塞尔/圆弧的细分段数

    // 端头预留（上下各 28）：融边指的爬升空间；悬浮时该区域透明
    public static float EndPad => (DockedHeight - FloatingHeight) * .5f;

    // t 形态下可见区距窗口顶/底的距离（悬浮=28，吸附=0）；拖动起点判定用
    public static float VisiblePad(float t) => EndPad * (1f - Math.Clamp(t, 0f, 1f));

    // 轮廓点列（加 offset 后的坐标；贴边侧按 x=0 构造，mirror 时 x 镜像到对侧）。
    // 点列从贴边中点起绕行一圈——起点保证轮廓对其星形可见，PathFillConvex 扇形填充精确。
    public static List<Vector2> BuildOutline(float t, bool mirror, float scale, Vector2 offset)
    {
        t = Math.Clamp(t, 0f, 1f);
        var w = Width * scale;
        var h = DockedHeight * scale;
        var r = w * .5f;
        var pad = EndPad * scale;
        var kh = HandleK * r;
        var cap = pad + r;                          // 端帽圆心高度
        var ky = cap * (1f - t);                    // 上贴边接触点 K：随 t 沿边爬升至顶
        var c1y = ky - kh * (1f - 2f * t);          // 首个手柄方向随 t 由朝上渐变为朝下

        var pts = new List<Vector2>(2 + Segments * 4 + 1);
        float X(float x) => mirror ? w - x : x;
        void Add(float x, float y) => pts.Add(offset + new Vector2(X(x), y));
        void Bezier(Vector2 p0, Vector2 q1, Vector2 q2, Vector2 p3)
        {
            for (var i = 1; i <= Segments; i++)
            {
                var u = i / (float)Segments;
                var v = 1f - u;
                var p = v * v * v * p0 + 3f * v * v * u * q1 + 3f * v * u * u * q2 + u * u * u * p3;
                Add(p.X, p.Y);
            }
        }
        void Arc(float cy, float a0) // 半径 r、圆心 (r, cy) 的四分之一弧，a0=-90°(上)/0°(下)
        {
            for (var i = 1; i <= Segments; i++)
            {
                var a = a0 + MathF.PI / 2f * (i / (float)Segments);
                Add(r + r * MathF.Cos(a), cy + r * MathF.Sin(a));
            }
        }

        Add(0, h * .5f);                        // 贴边中点（轮廓起点）
        Add(0, ky);                             // K：上贴边接触点
        Bezier(new(0, ky), new(0, c1y), new(r - kh, pad), new(r, pad)); // 上凹弧：离边竖直、汇入水平
        Arc(cap, -MathF.PI / 2f);               // 上内角四分之一弧 → (w, cap)
        Add(w, h - cap);                        // 内侧直边
        Arc(h - cap, 0f);                       // 下内角四分之一弧 → (r, h-pad)
        Bezier(new(r, h - pad), new(r - kh, h - pad), new(0, h - c1y), new(0, h - ky)); // 下凹弧（镜像）
        return pts;
    }
}

// 微欠阻尼弹簧（response 0.32s / damping 0.86）：单一标量驱动全部形变参数，速度连续、换向无突变。
// 用阻尼弹簧的解析解逐步推进——任意 dt 无条件稳定（卡顿帧不炸），且保留约 0.5% 的轻微过冲手感。
internal struct BerthSpring
{
    private const float Omega = MathF.PI * 2f / .32f;
    private const float Zeta = .86f;
    private static readonly float OmegaD = Omega * MathF.Sqrt(1f - Zeta * Zeta);

    public float Value;
    public float Velocity;

    public BerthSpring(float value)
    {
        Value = value;
        Velocity = 0f;
    }

    public float Step(float target, float dt)
    {
        dt = Math.Clamp(dt, 0f, .25f);
        if (dt <= 0f) return Value;
        var d = Value - target;
        var e = MathF.Exp(-Zeta * Omega * dt);
        var c = MathF.Cos(OmegaD * dt);
        var s = MathF.Sin(OmegaD * dt);
        Value = target + e * (d * c + (Velocity + Zeta * Omega * d) / OmegaD * s);
        Velocity = e * (Velocity * c - (Omega * Omega * d + Zeta * Omega * Velocity) / OmegaD * s);
        return Value;
    }
}
