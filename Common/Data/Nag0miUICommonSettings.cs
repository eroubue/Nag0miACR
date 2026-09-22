// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Text.Json;
using ECommons.Logging;

namespace Nag0mi.Common.Data;

// 多职业通用设置（单例，JSON 持久化到 Common.json，全部职业共用一份）。
// 只收录框架层通用内容；主题/配色相关字段在移植时已剔除（SimplePalette 另行移植）。
// 面板布局等职业专属配置在 Nag0miUISettings（按职业分文件）。
public class Nag0miUICommonSettings
{
    private static Nag0miUICommonSettings? instance;
    public static Nag0miUICommonSettings Instance => instance ??= Load();

    // ============================================================
    // === 窗口布局持久化（设置窗口族的位置/尺寸，键 = 窗口布局标识） ===
    // ============================================================
    public Dictionary<string, WindowLayoutData> WindowLayouts = new();

    // ============================================================
    // === 窗口个性化（字体 / 背景图, 全职业共用; 绘制逻辑在 Common/UI 的两个 Manager） ===
    // ============================================================
    public WindowFontSettings 设置窗口字体 = new();
    public WindowFontSettings Qt面板字体 = new();
    public WindowFontSettings 热键面板字体 = new();

    public WindowBackgroundSettings 设置窗口背景 = new() { BackgroundOpacity = 0.95f };
    public WindowBackgroundSettings Qt面板背景 = new() { BackgroundOpacity = 0.85f };
    public WindowBackgroundSettings 热键面板背景 = new() { BackgroundOpacity = 0.85f };

    // 水墨主题明暗模式（false=明/冷宣, true=暗/暖宣; 设置页「通用」切换, 运行期镜像在 ShuimoPalette.DarkMode）
    public bool DarkMode;

    // 单个窗口的持久化布局（JSON 可序列化；运行期状态在 SettingsWindowBase.WindowLayoutState）
    public sealed class WindowLayoutData
    {
        public System.Numerics.Vector2? Position;
        public System.Numerics.Vector2? Size;
    }

    // ============================================================
    // === JSON 持久化（路径与 Nag0miUISettings 同目录: Settings\ACRConfig\Nag0mi\Common.json） ===
    // ============================================================
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
    };

    public static string FilePath
    {
        get
        {
            var dir = Nag0miUISettings.SettingsDirectory;
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "Common.json");
        }
    }

    public static Nag0miUICommonSettings Load()
    {
        try
        {
            if (System.IO.File.Exists(FilePath))
            {
                var json = System.IO.File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<Nag0miUICommonSettings>(json, JsonOptions);
                if (s != null) return s;
            }
            else
            {
                // 首次使用: 按代码默认值初始化并立即落盘
                var s = new Nag0miUICommonSettings();
                s.Save();
                PluginLog.Log($"[{Nag0miUIJobEnv.作者}] 通用设置已初始化: {FilePath}");
                return s;
            }
        }
        catch (Exception e) { PluginLog.Error($"[{Nag0miUIJobEnv.作者}] 通用设置加载失败: {e.Message}"); }
        return new Nag0miUICommonSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, JsonOptions);
            var dir = System.IO.Path.GetDirectoryName(FilePath);
            if (dir != null && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(FilePath, json);
        }
        catch { /* 写失败静默 */ }
    }
}
