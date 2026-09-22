// Portions Copyright (c) shuimo-design, MIT License.
using System.Numerics;

namespace Nag0mi.Common.UI;

// 水墨色板：按 DarkMode 输出当前色值（色值取自上游参考实现的 var.css 原值）。
// 纯静态无外部依赖（单测直接编译本文件）；DarkMode 由设置页切换时写入、框架 Install 时
// 从持久化设置同步，各窗口每帧读取、无缓存快照，切换后下一帧全窗口生效。
public static class ShuimoPalette
{
    // 暗色模式（false=明/冷宣, true=暗/暖宣）。设置页切换时与 Nag0miUICommonSettings.DarkMode 同步写入。
    public static bool DarkMode { get; set; }

    // 主色：血红（激活/勾选/当前页/关闭/高难），明暗同值
    public static readonly Vector4 Main = Hex(0x861717);
    // 主色按下态（加深一档）
    public static readonly Vector4 MainPressed = Hex(0x6A1212);
    // 悬停：靛蓝；兼作自定义模式色
    public static readonly Vector4 Hover = Hex(0x1661AB);
    // 运行/启用：青
    public static readonly Vector4 Running = Hex(0x4A9992);
    // 暂停/警告：藤黄
    public static readonly Vector4 Hold = Hex(0xE8B004);
    // 关闭/高难：血红（同主色）
    public static readonly Vector4 Off = Main;
    // 自定义模式：靛蓝
    public static readonly Vector4 Custom = Hover;
    // 边框/分隔基色（明暗同值；暗色模式边框纹理以反色贴图承载）
    public static readonly Vector4 BorderBase = Hex(0x5E616D);
    // 控制条墨锭底：京元（深色实体, 不铺宣纸）
    public static readonly Vector4 InkStick = Hex(0x31322C);

    // 文字：明=墨 #2B333E / 暗=缟羽 #EEEEEE
    public static Vector4 Text => DarkMode ? Hex(0xEEEEEE) : Hex(0x2B333E);
    // 辅助文字：明=黑 / 暗=青鸾 #9AA7B1（明色灰字在宣纸灰底上对比度不足, 统一改黑）
    public static Vector4 TextSecondary => DarkMode ? Hex(0x9AA7B1) : Hex(0x000000);
    // 禁用文字：明=黑 / 暗=#5E616D
    public static Vector4 TextDisabled => DarkMode ? Hex(0x5E616D) : Hex(0x000000);

    // 控件底色（墨色淡染）：明=墨汁淡染 / 暗=缟羽淡染
    public static Vector4 FrameBg => DarkMode ? WithAlpha(Hex(0xEEEEEE), 0.10f) : WithAlpha(Hex(0x2B333E), 0.08f);
    public static Vector4 FrameBgHovered => DarkMode ? WithAlpha(Hex(0xEEEEEE), 0.14f) : WithAlpha(Hex(0x2B333E), 0.12f);
    public static Vector4 FrameBgActive => DarkMode ? WithAlpha(Hex(0xEEEEEE), 0.20f) : WithAlpha(Hex(0x2B333E), 0.16f);

    // 弹窗底（Combo 下拉/右键菜单; 近实底保可读性）：明=宣纸实色 / 暗=近黑实色
    public static Vector4 PopupBg => DarkMode ? new Vector4(0.04f, 0.035f, 0.03f, 0.98f) : new Vector4(0.949f, 0.933f, 0.894f, 0.98f);

    // 分隔线/描边（明低透明 / 暗略高透明）
    public static Vector4 Border => WithAlpha(BorderBase, DarkMode ? 0.55f : 0.45f);
    public static Vector4 BorderStrong => WithAlpha(BorderBase, DarkMode ? 0.75f : 0.65f);

    // 暗色模式纸底基色（黑底 + 暖宣纹理 50% 透明, 见 ShuimoDraw.DrawPaper）
    public static readonly Vector4 DarkBase = new(0f, 0f, 0f, 1f);

    public static Vector4 Hex(uint rgb)
        => new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

    public static Vector4 WithAlpha(Vector4 c, float a) => new(c.X, c.Y, c.Z, a);
}
