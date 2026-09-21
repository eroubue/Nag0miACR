// Portions Copyright (c) shuimo-design, MIT License.
using System.Numerics;

namespace Nag0mi.Common.UI;

// 框架窗口配色转发层：全部色值改指 ShuimoPalette（水墨色板, 明/暗随 DarkMode 切换），
// 现有调用点（状态色、模式色、QT 状态色、窗口全局样式 Push/Pop）签名不变。
public static class SimplePalette
{
    // 主色调（血红）
    public static Vector4 Primary => ShuimoPalette.Main;                  // #861717
    public static Vector4 PrimaryHover => ShuimoPalette.Hover;            // #1661AB 靛蓝
    public static Vector4 PrimaryActive => ShuimoPalette.MainPressed;     // #6A1212（加深一档）

    // 强调色：勾选、滑条 grab、导航激活
    public static Vector4 Accent => ShuimoPalette.Main;

    // 高难模式文字（血红）
    public static Vector4 ModeHighEndText => ShuimoPalette.Off;

    // 日随模式文字（青）
    public static Vector4 ModeDailyText => ShuimoPalette.Running;

    // 自定义模式（靛蓝, 控制条模式键）
    public static Vector4 ModeCustom => ShuimoPalette.Custom;

    // 文字
    public static Vector4 TextPrimary => ShuimoPalette.Text;
    public static Vector4 TextSecondary => ShuimoPalette.TextSecondary;
    public static Vector4 TextDisabled => ShuimoPalette.TextDisabled;

    public static Vector4 FrameBg => ShuimoPalette.FrameBg;
    public static Vector4 FrameBgHovered => ShuimoPalette.FrameBgHovered;
    public static Vector4 FrameBgActive => ShuimoPalette.FrameBgActive;

    // 弹窗底（Combo 下拉/右键菜单; 近实底保可读性）
    public static Vector4 PopupBg => ShuimoPalette.PopupBg;

    // 边框与分隔
    public static Vector4 Border => ShuimoPalette.Border;
    public static Vector4 BorderStrong => ShuimoPalette.BorderStrong;

    // 侧边栏导航激活项（血红淡染底 + 血红字; 侧边栏另画左侧血红竖条）
    public static Vector4 NavActiveBg => ShuimoPalette.WithAlpha(ShuimoPalette.Main, 0.18f);
    public static Vector4 NavActiveText => ShuimoPalette.Main;

    // 运行状态色（战斗控制窗口）
    public static Vector4 StateRunning => ShuimoPalette.Running;   // 青 #4A9992
    public static Vector4 StateHold => ShuimoPalette.Hold;         // 藤黄 #E8B004
    public static Vector4 StateOff => ShuimoPalette.Off;           // 血红 #861717

    // QT 瓦片状态色
    public static Vector4 QtOnBorder => ShuimoPalette.Running;                                // 启用：青色描边
    public static Vector4 QtOnGlow => ShuimoPalette.WithAlpha(ShuimoPalette.Running, 0.90f);  // 启用：底部微光条
    public static Vector4 QtOffBorder => ShuimoPalette.Off;                                   // 关闭：血红描边
    public static Vector4 QtOffVeil => new(0.10f, 0.10f, 0.10f, 0.35f);                       // 关闭：墨色罩层（半透明）

    // 控制条「模式」键取色：按模式名关键词映射（高难=血红 / 日随=青 / 自定义=靛蓝），未识别回落主色。
    public static Vector4 ModeButtonColor(string? modeName)
    {
        if (modeName == null) return ShuimoPalette.Main;
        if (modeName.Contains("高难")) return ShuimoPalette.Off;
        if (modeName.Contains("日随")) return ShuimoPalette.Running;
        if (modeName.Contains("自定义")) return ShuimoPalette.Custom;
        return ShuimoPalette.Main;
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
