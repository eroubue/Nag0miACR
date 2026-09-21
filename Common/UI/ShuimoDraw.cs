// Portions Copyright (c) shuimo-design, MIT License.
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using ECommons.DalamudServices;
using Nag0mi.Common.Data;

namespace Nag0mi.Common.UI;

// 水墨绘制原语：宣纸平铺 / 9-slice 笔触边框 / 山水装饰层 / 内置贴图缓存。
// 贴图随包内置发布（构建输出 UI\ 目录），视为恒存在：不做缺失检测、警告表、失败表，
// 异步解码就绪前 GetWrapOrDefault 返回 null 的帧跳过对应绘制即可。
public static class ShuimoDraw
{
    // ============================================================
    // === 内置贴图缓存（持有 ISharedImmediateTexture 保活, 绘制帧才取 wrap） ===
    // ============================================================
    private static readonly Dictionary<string, ISharedImmediateTexture> 贴图缓存 = new(StringComparer.OrdinalIgnoreCase);

    private static string? 贴图目录;

    // 贴图目录：ACR 程序集由宿主按字节流加载（Assembly.Location 为空）,
    // 目录约定为 宿主配置目录\ACR\作者\UI\（与安装包布局一致, csproj 输出目录同构）;
    // Assembly.Location 非空时兜底（开发直跑）。
    private static string? 取贴图目录()
    {
        if (贴图目录 != null) return 贴图目录;
        var candidates = new List<string?>(2)
        {
            Svc.PluginInterface.ConfigDirectory?.FullName is { } cfg
                ? Path.Combine(cfg, "ACR", Nag0miUIJobEnv.作者, "UI")
                : null,
            Path.GetDirectoryName(typeof(ShuimoDraw).Assembly.Location) is { Length: > 0 } asm
                ? Path.Combine(asm, "UI")
                : null,
        };
        foreach (var dir in candidates)
            if (dir != null && Directory.Exists(dir)) { 贴图目录 = dir; return dir; }
        return null;
    }

    // 取内置贴图（文件名相对 UI\ 目录）。未就绪/目录未找到时返回 null, 调用方当帧跳过。
    public static IDalamudTextureWrap? Tex(string fileName)
    {
        if (!贴图缓存.TryGetValue(fileName, out var tex))
        {
            var dir = 取贴图目录();
            if (dir == null) return null;
            try
            {
                tex = Svc.Texture.GetFromFile(Path.Combine(dir, fileName));
            }
            catch
            {
                return null;   // 视为恒存在, 不留失败表; 异常帧跳过, 下帧重试
            }
            贴图缓存[fileName] = tex;
        }
        return tex.GetWrapOrDefault(null);
    }

    /// <summary>清空纹理缓存（框架 Uninstall 时调用; 共享句柄释放计数由 Dalamud 托管）。</summary>
    public static void Dispose() => 贴图缓存.Clear();

    // 当前模式素材选择：暗色用反色笔触贴图（对应上游暗色 filter:invert()）
    public static IDalamudTextureWrap? 宣纸 => Tex(ShuimoPalette.DarkMode ? "paper_warm.png" : "paper_cold.png");
    public static IDalamudTextureWrap? 笔触边框 => Tex(ShuimoPalette.DarkMode ? "brush_border_inv.png" : "brush_border.png");
    public static IDalamudTextureWrap? 笔触勾选框 => Tex(ShuimoPalette.DarkMode ? "brush_checkbox_inv.png" : "brush_checkbox.png");
    public static IDalamudTextureWrap? 笔触墨点 => Tex(ShuimoPalette.DarkMode ? "brush_check_inv.png" : "brush_check.png");

    // ============================================================
    // === 宣纸平铺 ===
    // ============================================================
    // 明=冷宣平铺; 暗=黑底 + 暖宣 50% 透明平铺（对应上游暗色 ricePaper）。
    // alpha 跟随面板现有透明度惯例（设置窗 0.95, 悬浮面板 0.85）。调用方负责裁剪。
    public static void DrawPaper(ImDrawListPtr drawList, Vector2 min, Vector2 max, float alpha)
    {
        var dark = ShuimoPalette.DarkMode;
        if (dark)
            drawList.AddRectFilled(min, max, SimplePalette.ToU32(ShuimoPalette.DarkBase));

        var tex = 宣纸;
        if (tex == null || tex.Handle == IntPtr.Zero) return;

        var a = Math.Clamp(alpha * (dark ? 0.5f : 1f), 0f, 1f);
        var tint = SimplePalette.ToU32(new Vector4(1f, 1f, 1f, a));
        float tw = tex.Width, th = tex.Height;
        for (var y = min.Y; y < max.Y; y += th)
        for (var x = min.X; x < max.X; x += tw)
        {
            var ix = MathF.Min(x + tw, max.X);
            var iy = MathF.Min(y + th, max.Y);
            var uvMax = new Vector2((ix - x) / tw, (iy - y) / th);
            drawList.AddImage(tex.Handle, new Vector2(x, y), new Vector2(ix, iy), Vector2.Zero, uvMax, tint);
        }
    }

    // ============================================================
    // === 9-slice 笔触边框（对应 CSS border-image, slice=8） ===
    // ============================================================
    public const float 笔触Slice = 8f;

    // 通用 9-slice：四角固定 edge×edge, 边与中心拉伸。tint 为 AddImage 乘法染色（状态描边用）。
    public static void DrawNineSlice(ImDrawListPtr drawList, Vector2 min, Vector2 max,
        IDalamudTextureWrap tex, float edge, Vector4 tint)
    {
        if (tex.Handle == IntPtr.Zero) return;
        var col = SimplePalette.ToU32(tint);
        float tw = tex.Width, th = tex.Height;
        float su = 笔触Slice / tw, sv = 笔触Slice / th;
        var e = MathF.Min(edge, MathF.Min((max.X - min.X) * 0.5f, (max.Y - min.Y) * 0.5f));
        if (e <= 0f) return;

        float x0 = min.X, x1 = min.X + e, x2 = max.X - e, x3 = max.X;
        float y0 = min.Y, y1 = min.Y + e, y2 = max.Y - e, y3 = max.Y;
        var h = tex.Handle;

        // 四角
        drawList.AddImage(h, new Vector2(x0, y0), new Vector2(x1, y1), new Vector2(0f, 0f), new Vector2(su, sv), col);
        drawList.AddImage(h, new Vector2(x2, y0), new Vector2(x3, y1), new Vector2(1f - su, 0f), new Vector2(1f, sv), col);
        drawList.AddImage(h, new Vector2(x0, y2), new Vector2(x1, y3), new Vector2(0f, 1f - sv), new Vector2(su, 1f), col);
        drawList.AddImage(h, new Vector2(x2, y2), new Vector2(x3, y3), new Vector2(1f - su, 1f - sv), Vector2.One, col);
        // 四边
        drawList.AddImage(h, new Vector2(x1, y0), new Vector2(x2, y1), new Vector2(su, 0f), new Vector2(1f - su, sv), col);
        drawList.AddImage(h, new Vector2(x1, y2), new Vector2(x2, y3), new Vector2(su, 1f - sv), new Vector2(1f - su, 1f), col);
        drawList.AddImage(h, new Vector2(x0, y1), new Vector2(x1, y2), new Vector2(0f, sv), new Vector2(su, 1f - sv), col);
        drawList.AddImage(h, new Vector2(x2, y1), new Vector2(x3, y2), new Vector2(1f - su, sv), new Vector2(1f, 1f - sv), col);
        // 中心（透明底, 仅笔触纹理延伸; 不需要时可由调用方跳过）
        drawList.AddImage(h, new Vector2(x1, y1), new Vector2(x2, y2), new Vector2(su, sv), new Vector2(1f - su, 1f - sv), col);
    }

    // 窗口/面板外壳笔触边框（白色素描边, 暗色模式自动用反色贴图）
    public static void DrawBrushFrame(ImDrawListPtr drawList, Vector2 min, Vector2 max, float edge = 8f)
    {
        var tex = 笔触边框;
        if (tex != null)
            DrawNineSlice(drawList, min, max, tex, edge, Vector4.One);
    }

    // 状态描边：笔触边框按状态色染色（启用=青 / 关闭=血红）
    public static void DrawBrushStateFrame(ImDrawListPtr drawList, Vector2 min, Vector2 max, Vector4 stateColor, float edge = 5f)
    {
        var tex = 笔触边框;
        if (tex == null) return;
        // 染色用状态色 + 原贴图 alpha: 反色贴图为浅底深纹, 乘法染色后纹理呈状态色
        DrawNineSlice(drawList, min, max, tex, edge, stateColor);
    }

    // ============================================================
    // === 山水装饰层（设置窗非交互背景: 纸底之上、内容之下） ===
    // ============================================================
    // 五张山水: lf=左上 lm=左中 mlb=中下 rb=右下 rf=右远（定位语义与上游 ricePaper 一致）;
    // 暗色模式用暗色变体贴图。
    public static void DrawMountains(ImDrawListPtr drawList, Vector2 min, Vector2 max)
    {
        var dark = ShuimoPalette.DarkMode;
        var suf = dark ? "_d" : "";
        // 装饰层透明度：纸底之上隐约可见即可, 不抢内容可读性
        var alpha = dark ? 0.60f : 0.50f;
        var tint = SimplePalette.ToU32(new Vector4(1f, 1f, 1f, alpha));
        var w = max.X - min.X;
        var h = max.Y - min.Y;

        // lf 左上
        DrawMtn(drawList, Tex($"mtn_lf{suf}.png"), min, tint, w * 0.42f);
        // lm 左中（左边垂直居中偏上）
        var lm = Tex($"mtn_lm{suf}.png");
        if (lm != null)
        {
            var lw = w * 0.62f;
            var lh = lw * lm.Height / lm.Width;
            var pos = new Vector2(min.X, min.Y + h * 0.38f);
            drawList.AddImage(lm.Handle, pos, pos + new Vector2(lw, lh), Vector2.Zero, Vector2.One, tint);
        }
        // mlb 中下（底部, 横向居中偏左）
        var mlb = Tex($"mtn_mlb{suf}.png");
        if (mlb != null)
        {
            var mw = w * 0.80f;
            var mh = mw * mlb.Height / mlb.Width;
            var pos = new Vector2(min.X + w * 0.08f, max.Y - mh);
            drawList.AddImage(mlb.Handle, pos, pos + new Vector2(mw, mh), Vector2.Zero, Vector2.One, tint);
        }
        // rb 右下
        var rb = Tex($"mtn_rb{suf}.png");
        if (rb != null)
        {
            var rw = w * 0.72f;
            var rh = rw * rb.Height / rb.Width;
            var pos = new Vector2(max.X - rw, max.Y - rh);
            drawList.AddImage(rb.Handle, pos, pos + new Vector2(rw, rh), Vector2.Zero, Vector2.One, tint);
        }
        // rf 右远（右侧, 位于 rb 上方）
        var rf = Tex($"mtn_rf{suf}.png");
        if (rf != null)
        {
            var fw = w * 0.50f;
            var fh = fw * rf.Height / rf.Width;
            var pos = new Vector2(max.X - fw, max.Y - fh - h * 0.22f);
            drawList.AddImage(rf.Handle, pos, pos + new Vector2(fw, fh), Vector2.Zero, Vector2.One, tint);
        }
    }

    // 左上锚定绘制单张山水（宽随窗口比例, 等比缩放）
    private static void DrawMtn(ImDrawListPtr drawList, IDalamudTextureWrap? tex, Vector2 pos, uint tint, float width)
    {
        if (tex == null) return;
        var height = width * tex.Height / tex.Width;
        drawList.AddImage(tex.Handle, pos, pos + new Vector2(width, height), Vector2.Zero, Vector2.One, tint);
    }
}
