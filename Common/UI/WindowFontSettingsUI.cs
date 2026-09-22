using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.GameFonts;
using Nag0mi.Common.Data;

namespace Nag0mi.Common.UI;

// 窗口字体设置 UI：逐窗口（设置窗口/QT面板/热键面板）的字体开关、类型、字号与预览。
// 全部改动即时生效（下一帧绘制即用新字体）, 改动后经 saveAction 落盘。
public static class WindowFontSettingsUI
{
    /// <summary>绘制单个窗口的字体设置（折叠节）。</summary>
    public static void DrawWindowFontSettings(string windowName, WindowFontSettings settings, Action? saveAction = null)
    {
        ImGui.PushID($"font_settings_{windowName}");

        if (ImGui.CollapsingHeader($"{windowName} 字体"))
        {
            ImGui.Indent(10f);

            if (ImGui.Checkbox("启用自定义字体", ref settings.EnableCustomFont))
            {
                if (!settings.EnableCustomFont)
                    WindowFontManager.ClearCustomFont(settings);
                saveAction?.Invoke();
            }

            if (!settings.EnableCustomFont)
            {
                ImGui.TextColored(SimplePalette.TextSecondary, "使用默认字体");
            }
            else
            {
                DrawFontTypeSettings(settings, saveAction);
                ImGui.Spacing();
                DrawFontSizeSettings(settings, saveAction);
                ImGui.Spacing();
                DrawFontPreview(settings);
            }

            ImGui.Unindent(10f);
        }

        ImGui.PopID();
    }

    private static void DrawFontTypeSettings(WindowFontSettings settings, Action? saveAction)
    {
        if (ImGui.RadioButton("系统字体", settings.FontType == FontType.SystemFont))
        {
            settings.FontType = FontType.SystemFont;
            saveAction?.Invoke();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("默认字体的指定字号");

        if (ImGui.RadioButton("游戏字体", settings.FontType == FontType.GameFont))
        {
            settings.FontType = FontType.GameFont;
            saveAction?.Invoke();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("游戏内字体");

        if (ImGui.RadioButton("自定义字体文件", settings.FontType == FontType.CustomFont))
        {
            settings.FontType = FontType.CustomFont;
            saveAction?.Invoke();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("从文件加载");

        switch (settings.FontType)
        {
            case FontType.GameFont:
                DrawGameFontSettings(settings, saveAction);
                break;
            case FontType.CustomFont:
                DrawCustomFontSettings(settings, saveAction);
                break;
        }
    }

    private static void DrawGameFontSettings(WindowFontSettings settings, Action? saveAction)
    {
        ImGui.Indent(10f);
        ImGui.Text("游戏字体族:");
        ImGui.SetNextItemWidth(200f);

        var current = (int)settings.GameFontFamily;
        var names = Enum.GetNames<GameFontFamily>();
        if (ImGui.Combo("##游戏字体族", ref current, names, names.Length))
        {
            settings.GameFontFamily = (GameFontFamily)current;
            saveAction?.Invoke();
        }
        ImGui.Unindent(10f);
    }

    private static void DrawCustomFontSettings(WindowFontSettings settings, Action? saveAction)
    {
        ImGui.Indent(10f);

        if (!string.IsNullOrEmpty(settings.CustomFontPath))
        {
            ImGui.Text("当前字体文件:");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Path.GetFileName(settings.CustomFontPath));
            ImGui.SameLine();
            if (ImGui.SmallButton("清除"))
            {
                WindowFontManager.ClearCustomFont(settings);
                saveAction?.Invoke();
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(1f, 0.7f, 0f, 1f), "未选择字体文件");
        }

        if (ImGui.Button("选择字体文件"))
        {
            // 文件对话框阻塞, 放后台线程; 设置对象为引用类型, 回来后直接改即生效
            Task.Run(() =>
            {
                var filePath = WindowFontManager.OpenFontFileDialog();
                if (!string.IsNullOrEmpty(filePath))
                {
                    WindowFontManager.SetCustomFont(settings, filePath);
                    saveAction?.Invoke();
                }
            });
        }
        ImGui.SameLine();
        ImGui.TextDisabled("支持: TTF, OTF, TTC, WOFF");

        ImGui.Unindent(10f);
    }

    private static void DrawFontSizeSettings(WindowFontSettings settings, Action? saveAction)
    {
        ImGui.Text("字体大小:");
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderFloat("##字体大小", ref settings.FontSize, 8f, 48f, "%.0f px"))
        {
            settings.FontSize = Math.Clamp(settings.FontSize, 8f, 48f);
            saveAction?.Invoke();
        }

        ImGui.Text("快速设置:");
        foreach (var (label, size) in new[] { ("小", 12f), ("中", 16f), ("大", 20f), ("特大", 24f) })
        {
            ImGui.SameLine();
            if (ImGui.SmallButton(label))
            {
                settings.FontSize = size;
                saveAction?.Invoke();
            }
        }
    }

    private static void DrawFontPreview(WindowFontSettings settings)
    {
        ImGui.BeginChild("font_preview", new Vector2(0, 80), true);
        WindowFontManager.DrawWithCustomFont(settings, "预览", () =>
        {
            ImGui.Text("预览文字 ABC 123 测试字体效果");
            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "This is a font preview");
        });
        ImGui.EndChild();

        ImGui.TextColored(SimplePalette.TextSecondary,
            $"类型: {settings.FontType switch
            {
                FontType.SystemFont => "系统字体",
                FontType.GameFont => "游戏字体",
                FontType.CustomFont => "自定义字体",
                _ => "未知",
            }}   大小: {settings.FontSize:F0}px");
    }

    /// <summary>绘制三个窗口的字体设置总览 + 全局操作。</summary>
    public static void DrawAllWindowFontSettings(
        WindowFontSettings settingsWindow,
        WindowFontSettings qtPanel,
        WindowFontSettings hotkeyPanel,
        Action? saveAction = null)
    {
        if (ImGui.Button("重置所有字体"))
        {
            WindowFontManager.ClearCustomFont(settingsWindow);
            WindowFontManager.ClearCustomFont(qtPanel);
            WindowFontManager.ClearCustomFont(hotkeyPanel);
            saveAction?.Invoke();
        }

        ImGui.SameLine();
        if (ImGui.Button("应用统一字体"))
        {
            // 取第一个启用了自定义字体的窗口作为模板, 复制到全部窗口
            var source = settingsWindow.EnableCustomFont ? settingsWindow
                       : qtPanel.EnableCustomFont ? qtPanel
                       : hotkeyPanel.EnableCustomFont ? hotkeyPanel : null;
            if (source != null)
            {
                foreach (var target in new[] { settingsWindow, qtPanel, hotkeyPanel })
                {
                    target.EnableCustomFont = source.EnableCustomFont;
                    target.FontType = source.FontType;
                    target.FontSize = source.FontSize;
                    target.CustomFontPath = source.CustomFontPath;
                    target.GameFontFamily = source.GameFontFamily;
                }
                saveAction?.Invoke();
            }
        }

        ImGui.Separator();

        DrawWindowFontSettings("设置窗口", settingsWindow, saveAction);
        DrawWindowFontSettings("QT面板", qtPanel, saveAction);
        DrawWindowFontSettings("热键面板", hotkeyPanel, saveAction);

        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "使用提示:");
        ImGui.BulletText("支持 TTF/OTF/TTC/WOFF 等常见字体格式");
        ImGui.BulletText("字体大小建议在 12-24px 之间以获得最佳显示效果");
        ImGui.BulletText("字体变更下一帧即生效, 无需重启");
    }
}
