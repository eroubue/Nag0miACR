// Ma Shan Zheng 马善政毛笔体, SIL Open Font License 1.1（许可全文见 UI\MaShanZheng-OFL.txt）。
using Dalamud.Interface.ManagedFontAtlas;
using ECommons.DalamudServices;
using Nag0mi.Common.Data;

namespace Nag0mi.Common.UI;

// 水墨标题书法字体（Ma Shan Zheng, 随包内置 GB2312 一级子集）：只用于标题位
// （窗口标题 / 侧边栏页签 / 分节标题），正文/控件文字沿用宿主默认字体。
// 句柄懒加载一次并缓存；构建完成前返回未就绪句柄, Push 内自动回落默认字体。
public static class ShuimoFont
{
    private const float 标题字号 = 22f;

    private static IFontHandle? handle;
    private static byte[]? fontData;

    /// <summary>标题字体句柄（文件读取或句柄创建失败时返回 null, 调用方回落默认字体绘制）。</summary>
    public static IFontHandle? Title
    {
        get
        {
            if (handle != null) return handle;
            try
            {
                fontData ??= 读字体字节();
                if (fontData == null) return null;
                var data = fontData;
                handle = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
                    e.OnPreBuild(tk => tk.AddFontFromMemory(
                        span: data.AsSpan(),
                        debugTag: "Nag0miShuimoTitle:MaShanZheng-Regular.ttf",
                        fontConfig: new SafeFontConfig
                        {
                            SizePx = 标题字号,
                            GlyphRanges = GlyphRanges,
                        })));
            }
            catch (Exception ex)
            {
                Svc.Log.Debug($"[{Nag0miUIJobEnv.作者}] 标题书法字体创建失败: {ex.Message}");
            }
            return handle;
        }
    }

    // 从内置 UI 目录读字体字节（与 ShuimoDraw 同一目录约定）
    private static byte[]? 读字体字节()
    {
        try
        {
            var candidates = new List<string?>(2)
            {
                Svc.PluginInterface.ConfigDirectory?.FullName is { } cfg
                    ? Path.Combine(cfg, "ACR", Nag0miUIJobEnv.作者, "UI", "MaShanZheng-Regular.ttf")
                    : null,
                Path.GetDirectoryName(typeof(ShuimoFont).Assembly.Location) is { Length: > 0 } asm
                    ? Path.Combine(asm, "UI", "MaShanZheng-Regular.ttf")
                    : null,
            };
            foreach (var path in candidates)
                if (path != null && File.Exists(path))
                    return File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"[{Nag0miUIJobEnv.作者}] 标题书法字体读取失败: {ex.Message}");
        }
        return null;
    }

    // 字形范围：拉丁 + 常用标点 + CJK（与 WindowFontManager 同口径; 缺省仅基础拉丁, 中文渲染成 ?）
    private static ushort[] GlyphRanges =>
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

    /// <summary>释放字体句柄（框架 Uninstall 时调用）。</summary>
    public static void Dispose()
    {
        try { handle?.Dispose(); } catch { /* 宿主可能已销毁 */ }
        handle = null;
    }
}
