using System.Numerics;
using Dalamud.Bindings.ImGui;
using Nag0mi.Common.Data;

namespace Nag0mi.Common.UI;

// 窗口背景图设置 UI：逐窗口（设置窗口/QT面板/热键面板）的背景图开关、选图与透明度。
// 改动即时生效（下一帧绘制即用新背景）, 改动后经 saveAction 落盘。
public static class WindowBackgroundSettingsUI
{
    /// <summary>绘制单个窗口的背景设置（折叠节）。</summary>
    public static void DrawWindowBackgroundSettings(string windowName, WindowBackgroundSettings settings, Action? saveAction = null)
    {
        ImGui.PushID($"background_settings_{windowName}");

        if (ImGui.CollapsingHeader($"{windowName} 背景"))
        {
            ImGui.Indent(10f);

            if (ImGui.Checkbox("启用背景图片", ref settings.EnableBackgroundImage))
                saveAction?.Invoke();

            if (settings.EnableBackgroundImage)
            {
                ImGui.Indent(10f);

                if (!string.IsNullOrEmpty(settings.BackgroundImagePath))
                {
                    ImGui.Text("当前图片:");
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Path.GetFileName(settings.BackgroundImagePath));
                    ImGui.SameLine();
                    if (ImGui.SmallButton("清除"))
                    {
                        WindowBackgroundManager.ClearBackgroundImage(settings);
                        saveAction?.Invoke();
                    }
                }
                else
                {
                    ImGui.TextColored(new Vector4(1f, 0.7f, 0f, 1f), "未设置背景图片");
                }

                if (ImGui.Button("选择图片文件"))
                {
                    // 文件对话框阻塞, 放后台线程; 设置对象为引用类型, 回来后直接改即生效
                    Task.Run(() =>
                    {
                        var filePath = WindowBackgroundManager.OpenBackgroundImageFileDialog();
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            WindowBackgroundManager.SetBackgroundImage(settings, filePath);
                            saveAction?.Invoke();
                        }
                    });
                }
                ImGui.SameLine();
                ImGui.TextDisabled("支持: PNG/JPG/BMP/WebP");

                ImGui.Text("图片透明度:");
                ImGui.SetNextItemWidth(200f);
                if (ImGui.SliderFloat("##背景透明度", ref settings.BackgroundImageOpacity, 0.0f, 1.0f, "%.2f"))
                    saveAction?.Invoke();

                ImGui.Text("快速设置:");
                foreach (var (label, opacity) in new[] { ("10%", 0.1f), ("30%", 0.3f), ("50%", 0.5f), ("70%", 0.7f), ("100%", 1.0f) })
                {
                    ImGui.SameLine();
                    if (ImGui.SmallButton(label))
                    {
                        settings.BackgroundImageOpacity = opacity;
                        saveAction?.Invoke();
                    }
                }

                ImGui.Unindent(10f);
            }

            ImGui.Unindent(10f);
        }

        ImGui.PopID();
    }

    /// <summary>绘制三个窗口的背景设置总览 + 全局操作。</summary>
    public static void DrawAllWindowBackgroundSettings(
        WindowBackgroundSettings settingsWindow,
        WindowBackgroundSettings qtPanel,
        WindowBackgroundSettings hotkeyPanel,
        Action? saveAction = null)
    {
        if (ImGui.Button("清除所有背景"))
        {
            WindowBackgroundManager.ClearBackgroundImage(settingsWindow);
            WindowBackgroundManager.ClearBackgroundImage(qtPanel);
            WindowBackgroundManager.ClearBackgroundImage(hotkeyPanel);
            saveAction?.Invoke();
        }

        ImGui.Separator();

        DrawWindowBackgroundSettings("设置窗口", settingsWindow, saveAction);
        DrawWindowBackgroundSettings("QT面板", qtPanel, saveAction);
        DrawWindowBackgroundSettings("热键面板", hotkeyPanel, saveAction);

        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "使用提示:");
        ImGui.BulletText("支持 PNG/JPG/BMP/WebP 等常见图片格式");
        ImGui.BulletText("图片自动保持宽高比并居中裁切铺满窗口");
        ImGui.BulletText("建议使用分辨率适中的图片以获得最佳效果");
    }
}
