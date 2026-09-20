// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;

namespace Nag0mi.Common.UI;

// 框架窗口配色：只保留 ErosUI 的 Dark（深底浅字）一套，主题切换与主色自定义已剔除。
// 只用于窗口全局样式 Push/Pop 与面板自绘取色。
public static class SimplePalette
{
    // 主色调
    public static readonly Vector4 Primary = new(0.686f, 0.318f, 0.914f, 1f);       // #AF51E9
    public static readonly Vector4 PrimaryHover = new(0.749f, 0.454f, 0.931f, 1f);  // #BF74ED（提亮一档）
    public static readonly Vector4 PrimaryActive = new(0.604f, 0.280f, 0.804f, 1f); // #9A47CD（加深一档）

    // 强调色：勾选、滑条 grab、导航激活
    public static Vector4 Accent => Primary;

    // 高难模式文字（亮橙）
    public static readonly Vector4 ModeHighEndText = new(1f, 0.80f, 0.20f, 1f);

    // 日随模式文字（亮绿）
    public static readonly Vector4 ModeDailyText = new(0.20f, 1f, 0.40f, 1f);

    // 自定义模式黄（控制条模式键）
    public static readonly Vector4 ModeCustom = new(0.98f, 0.84f, 0.28f, 1f);

    // 文字
    public static readonly Vector4 TextPrimary = new(0.95f, 0.95f, 0.95f, 1f);
    public static readonly Vector4 TextSecondary = new(0.70f, 0.70f, 0.70f, 1f);
    public static readonly Vector4 TextDisabled = new(0.35f, 0.35f, 0.35f, 1f);

    public static readonly Vector4 FrameBg = new(0.10f, 0.10f, 0.10f, 0.50f);
    public static readonly Vector4 FrameBgHovered = new(0.15f, 0.15f, 0.15f, 0.60f);
    public static readonly Vector4 FrameBgActive = new(0.20f, 0.20f, 0.20f, 0.70f);

    // 弹窗底（Combo 下拉/右键菜单; 近实底保可读性）
    public static readonly Vector4 PopupBg = new(0.06f, 0.06f, 0.06f, 0.98f);

    // 边框与分隔
    public static readonly Vector4 Border = new(1f, 1f, 1f, 0.10f);
    public static readonly Vector4 BorderStrong = new(1f, 1f, 1f, 0.20f);

    // 侧边栏导航激活项
    public static readonly Vector4 NavActiveBg = new(Primary.X, Primary.Y, Primary.Z, 0.18f);
    public static readonly Vector4 NavActiveText = new(0.95f, 0.95f, 0.95f, 1f);

    // 运行状态色（战斗控制窗口）
    public static readonly Vector4 StateRunning = new(0.30f, 0.85f, 0.45f, 1f);   // 绿
    public static readonly Vector4 StateHold = new(0.96f, 0.62f, 0.20f, 1f);       // 橙
    public static readonly Vector4 StateOff = new(0.92f, 0.30f, 0.32f, 1f);        // 红

    // QT 瓦片状态色
    public static readonly Vector4 QtOnBorder = new(0.30f, 0.85f, 0.45f, 1f);        // 启用：绿色描边
    public static readonly Vector4 QtOnGlow = new(0.30f, 0.85f, 0.45f, 0.90f);       // 启用：底部微光条
    public static readonly Vector4 QtOffBorder = new(0.92f, 0.30f, 0.32f, 1f);       // 关闭：红色描边
    public static readonly Vector4 QtOffVeil = new(0.08f, 0.08f, 0.10f, 0.38f);      // 关闭：深灰罩层（半透明）

    // 控制条「模式」键取色：按模式名关键词映射（高难=红 / 日随=绿 / 自定义=黄），未识别回落主色。
    public static Vector4 ModeButtonColor(string? modeName)
    {
        if (modeName == null) return PrimaryHover;
        if (modeName.Contains("高难")) return StateOff;
        if (modeName.Contains("日随")) return StateRunning;
        if (modeName.Contains("自定义")) return ModeCustom;
        return PrimaryHover;
    }

    public static uint ToU32(Vector4 c)
    {
        var r = (byte)(Math.Clamp(c.X, 0f, 1f) * 255);
        var g = (byte)(Math.Clamp(c.Y, 0f, 1f) * 255);
        var b = (byte)(Math.Clamp(c.Z, 0f, 1f) * 255);
        var a = (byte)(Math.Clamp(c.W, 0f, 1f) * 255);
        return (uint)((a << 24) | (b << 16) | (g << 8) | r);
    }

    public static Vector4 WithAlpha(Vector4 c, float a) => new(c.X, c.Y, c.Z, a);
}
