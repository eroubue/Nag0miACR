using Dalamud.Interface.GameFonts;

namespace Nag0mi.Common.Data;

// 窗口个性化设置的数据载体（字体 / 背景图），随 Nag0miUICommonSettings 落盘（全职业共用）。
// 绘制与加载逻辑在 Common/UI 的 WindowFontManager / WindowBackgroundManager。

/// <summary>字体类型</summary>
public enum FontType
{
    SystemFont,
    GameFont,
    CustomFont,
}

/// <summary>单个窗口的字体设置</summary>
public class WindowFontSettings
{
    /// 是否启用自定义字体（false = 宿主默认字体）
    public bool EnableCustomFont = false;

    /// 字体类型
    public FontType FontType = FontType.SystemFont;

    /// 字体大小(px)
    public float FontSize = 16f;

    /// 自定义字体文件路径
    public string CustomFontPath = "";

    /// 游戏字体族
    public GameFontFamily GameFontFamily = GameFontFamily.Axis;
}

/// <summary>单个窗口的背景图设置</summary>
public class WindowBackgroundSettings
{
    /// 是否启用背景图片
    public bool EnableBackgroundImage = false;

    /// 背景图片路径
    public string BackgroundImagePath = "";

    /// 背景图片透明度 (0-1)
    public float BackgroundImageOpacity = 0.3f;

    /// 默认宣纸底不透明度 (0-1；0 = 完全无底色，文字悬浮于游戏画面)。
    /// 真实不透明度：1 = 完全不透明（明色模式垫冷宣实色底托底, 见 ShuimoDraw.DrawPaper）。
    /// 仅默认底色生效；启用自定义背景图时由 BackgroundImageOpacity 接管。
    public float BackgroundOpacity = 1f;
}
