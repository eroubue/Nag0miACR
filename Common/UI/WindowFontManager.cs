using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using ECommons.DalamudServices;
using Nag0mi.Common.Data;
using Nag0mi.Common.Helper;

namespace Nag0mi.Common.UI;

// 窗口字体管理器：统一处理各悬浮窗/设置窗的字体加载与应用。
// 字体句柄按 (来源, 字号) 缓存，Uninstall 时统一 Dispose；
// 任何加载/应用失败都静默回落宿主默认字体，绝不影响窗口绘制。
public static class WindowFontManager
{
    private static readonly Dictionary<string, IFontHandle> FontCache = new(StringComparer.Ordinal);
    private static readonly Dictionary<(GameFontFamily, float), IFontHandle> GameFontCache = new();

    /// <summary>以自定义字体绘制内容；未启用或加载失败时直接用默认字体绘制。</summary>
    public static void DrawWithCustomFont(WindowFontSettings settings, string windowType, Action drawContent)
    {
        IFontHandle? fontHandle = null;
        try
        {
            if (settings.EnableCustomFont)
                fontHandle = GetFontHandle(settings, windowType);

            if (fontHandle != null)
            {
                using (fontHandle.Push())
                    drawContent();
            }
            else
            {
                drawContent();
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"[Nag0mi] 应用{windowType}字体时发生错误: {ex.Message}");
            drawContent();
        }
    }

    private static IFontHandle? GetFontHandle(WindowFontSettings settings, string windowType)
    {
        try
        {
            return settings.FontType switch
            {
                FontType.SystemFont => GetSystemFont(settings.FontSize),
                FontType.GameFont => GetGameFont(settings.GameFontFamily, settings.FontSize),
                FontType.CustomFont when !string.IsNullOrEmpty(settings.CustomFontPath)
                    => GetCustomFont(settings.CustomFontPath, settings.FontSize),
                _ => null,
            };
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"[Nag0mi] 获取{windowType}字体句柄失败: {ex.Message}");
            return null;
        }
    }

    // 系统字体：宿主默认字体的指定字号变体
    private static IFontHandle? GetSystemFont(float fontSize)
    {
        if (fontSize <= 0) return null;

        var key = $"system_{fontSize}";
        if (FontCache.TryGetValue(key, out var cached)) return cached;

        try
        {
            var handle = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
                e.OnPreBuild(tk => tk.AddDalamudDefaultFont(fontSize)));
            if (handle != null) FontCache[key] = handle;
            return handle;
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"[Nag0mi] 创建系统字体失败: {ex.Message}");
            return null;
        }
    }

    // 游戏字体：按 (字体族, 字号) 缓存——同族不同字号是不同句柄
    private static IFontHandle? GetGameFont(GameFontFamily fontFamily, float fontSize)
    {
        if (fontSize <= 0) return null;

        var key = (fontFamily, fontSize);
        if (GameFontCache.TryGetValue(key, out var cached)) return cached;

        try
        {
            var handle = Svc.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(
                new GameFontStyle(fontFamily, fontSize));
            if (handle != null) GameFontCache[key] = handle;
            return handle;
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"[Nag0mi] 创建游戏字体失败 ({fontFamily}): {ex.Message}");
            return null;
        }
    }

    private static IFontHandle? GetCustomFont(string fontPath, float fontSize)
    {
        if (string.IsNullOrEmpty(fontPath) || fontSize <= 0) return null;

        var key = $"{fontPath}_{fontSize}";
        if (FontCache.TryGetValue(key, out var cached)) return cached;

        if (!File.Exists(fontPath))
        {
            Svc.Log.Debug($"[Nag0mi] 字体文件不存在: {fontPath}");
            return null;
        }

        try
        {
            // 字节读入内存后再交给字体图集：句柄重建（字号变更等）时从内存重加，不重复读盘
            byte[] fontData = File.ReadAllBytes(fontPath);
            var handle = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
                e.OnPreBuild(tk => tk.AddFontFromMemory(
                    span: fontData.AsSpan(),
                    debugTag: $"Nag0miCustomFont:{Path.GetFileName(fontPath)}",
                    fontConfig: new SafeFontConfig
                    {
                        SizePx = fontSize,
                        GlyphRanges = GetGlyphRanges(),
                    })));
            if (handle != null) FontCache[key] = handle;
            return handle;
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"[Nag0mi] 加载自定义字体失败 ({fontPath}): {ex.Message}");
            return null;
        }
    }

    // 字形范围：拉丁 + 常用标点 + CJK。缺省仅基础拉丁, 中文会渲染成 ?
    private static ushort[] GetGlyphRanges() =>
    [
        0x0020, 0x00FF, // Basic Latin + Latin-1 Supplement
        0x2000, 0x206F, // General Punctuation
        0x3000, 0x303F, // CJK Symbols and Punctuation
        0x3400, 0x4DBF, // CJK Unified Ideographs Extension A
        0x4E00, 0x9FFF, // CJK Unified Ideographs
        0xF900, 0xFAFF, // CJK Compatibility Ideographs
        0xFF00, 0xFFEF, // Halfwidth and Fullwidth Forms
        0,              // terminator
    ];

    /// <summary>设置自定义字体文件（文件不存在时不改动设置）。</summary>
    public static void SetCustomFont(WindowFontSettings settings, string fontPath)
    {
        if (string.IsNullOrEmpty(fontPath) || !File.Exists(fontPath))
        {
            Svc.Log.Error($"[Nag0mi] 字体文件不存在: {fontPath}");
            return;
        }
        settings.FontType = FontType.CustomFont;
        settings.CustomFontPath = fontPath;
        settings.EnableCustomFont = true;
    }

    /// <summary>恢复默认字体。</summary>
    public static void ClearCustomFont(WindowFontSettings settings)
    {
        settings.EnableCustomFont = false;
        settings.FontType = FontType.SystemFont;
        settings.CustomFontPath = "";
    }

    /// <summary>打开字体文件选择对话框（阻塞, 调用方包 Task.Run）。</summary>
    public static string? OpenFontFileDialog()
        => Win32FileDialog.OpenFile("选择字体文件",
            "字体文件\0*.ttf;*.otf;*.ttc;*.woff;*.woff2\0所有文件\0*.*\0");

    /// <summary>清理全部字体缓存（框架 Uninstall 时调用）。</summary>
    public static void Dispose()
    {
        try
        {
            foreach (var font in FontCache.Values) font?.Dispose();
            FontCache.Clear();
            foreach (var font in GameFontCache.Values) font?.Dispose();
            GameFontCache.Clear();
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"[Nag0mi] 清理字体缓存时发生错误: {ex.Message}");
        }
    }
}
