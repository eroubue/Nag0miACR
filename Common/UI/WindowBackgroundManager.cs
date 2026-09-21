using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using ECommons.DalamudServices;
using Nag0mi.Common.Data;
using Nag0mi.Common.Helper;

namespace Nag0mi.Common.UI;

// 窗口背景图管理器：图片加载（共享纹理缓存）与等比裁切铺满绘制。
// 纹理缓存与热键面板贴图同一套模式：持有 ISharedImmediateTexture 保活，
// 绘制帧才取 wrap，避免缓存已销毁的 wrap 句柄。
public static class WindowBackgroundManager
{
    private static readonly Dictionary<string, ISharedImmediateTexture> TextureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> LoadFailed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 在指定区域绘制背景图（等比缩放居中裁切铺满, 圆角随窗口）。
    /// 未启用/路径为空/加载失败时不画任何东西并返回 false（调用方画默认底色）。
    /// </summary>
    public static bool DrawBackgroundImage(ImDrawListPtr drawList, WindowBackgroundSettings settings,
        Vector2 pos, Vector2 size, float rounding)
    {
        if (!settings.EnableBackgroundImage || string.IsNullOrEmpty(settings.BackgroundImagePath))
            return false;

        var tex = LoadTexture(settings.BackgroundImagePath);
        if (tex == null) return false;

        try
        {
            var handle = tex.Handle;
            if (handle == IntPtr.Zero) return false;

            // 等比 cover：按窗口/图片宽高比决定裁剪方向, UV 居中裁切
            var aspectImage = tex.Width / (float)tex.Height;
            var aspectWindow = size.X / size.Y;
            float scaledW, scaledH;
            if (aspectImage > aspectWindow)
            {
                scaledH = size.Y;
                scaledW = scaledH * aspectImage;
            }
            else
            {
                scaledW = size.X;
                scaledH = scaledW / aspectImage;
            }

            var cropX = Math.Max(0f, (scaledW - size.X) * 0.5f / scaledW);
            var cropY = Math.Max(0f, (scaledH - size.Y) * 0.5f / scaledH);
            var uvMin = new Vector2(cropX, cropY);
            var uvMax = new Vector2(1f - cropX, 1f - cropY);

            var opacity = Math.Clamp(settings.BackgroundImageOpacity, 0f, 1f);
            var alpha = (uint)(opacity * 255);
            var tint = 0xFF000000u | (alpha << 16) | (alpha << 8) | alpha;

            drawList.AddImageRounded(handle, pos, pos + size, uvMin, uvMax, tint, rounding);
            return true;
        }
        catch (Exception ex)
        {
            Svc.Log.Debug($"[Nag0mi] 绘制背景图片时发生错误: {ex.Message}");
            return false;
        }
    }

    // 加载图片为共享纹理（缓存命中直接返回; 失败过的路径不再重试, 换图需重选文件）。
    // 异步解码就绪前 GetWrapOrDefault 返回 null, 下帧自动重试。
    private static IDalamudTextureWrap? LoadTexture(string path)
    {
        if (LoadFailed.Contains(path)) return null;
        if (!TextureCache.TryGetValue(path, out var tex))
        {
            if (!File.Exists(path))
            {
                Svc.Log.Debug($"[Nag0mi] 背景图片文件不存在: {path}");
                return null;   // 文件可能稍后才就位（如配置同步）, 不记入失败表
            }
            try
            {
                tex = Svc.Texture.GetFromFile(path);
            }
            catch (Exception ex)
            {
                LoadFailed.Add(path);
                Svc.Log.Warning($"[Nag0mi] 背景图片加载失败 {path}: {ex.Message}");
                return null;
            }
            TextureCache[path] = tex;
        }
        return tex.GetWrapOrDefault(null);
    }

    /// <summary>设置背景图片（文件不存在时不改动设置）。换图时丢弃旧路径的缓存与失败记录。</summary>
    public static void SetBackgroundImage(WindowBackgroundSettings settings, string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            Svc.Log.Error($"[Nag0mi] 背景图片文件不存在: {imagePath}");
            return;
        }
        if (!string.Equals(settings.BackgroundImagePath, imagePath, StringComparison.OrdinalIgnoreCase))
        {
            TextureCache.Remove(settings.BackgroundImagePath);
            LoadFailed.Remove(imagePath);
        }
        settings.BackgroundImagePath = imagePath;
        settings.EnableBackgroundImage = true;
    }

    /// <summary>清除背景图片设置。</summary>
    public static void ClearBackgroundImage(WindowBackgroundSettings settings)
    {
        settings.EnableBackgroundImage = false;
        settings.BackgroundImagePath = "";
    }

    /// <summary>打开背景图片选择对话框（阻塞, 调用方包 Task.Run）。</summary>
    public static string? OpenBackgroundImageFileDialog()
        => Win32FileDialog.OpenFile("选择背景图片",
            "图片文件\0*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.webp\0所有文件\0*.*\0");

    /// <summary>清空纹理缓存（框架 Uninstall 时调用; 共享句柄释放计数由 Dalamud 托管）。</summary>
    public static void Dispose()
    {
        TextureCache.Clear();
        LoadFailed.Clear();
    }
}
