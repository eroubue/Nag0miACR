// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Nag0mi.Common.UI;

// 侧边栏行样式：控件在左、标签在右；节标题前置主题色圆点。
// 颜色统一取 SimplePalette 暗色方案，本类不含硬编码颜色。
// 仅由 SettingRow 分发调用，内容代码不直接使用本类。
public static class SidebarSettingRow
{
    // 分节标题：主题色圆点 + 书法字体标题 + 右侧细线。
    public static void SectionTitle(string title)
    {
        ImGui.Spacing();

        // 分节标题属标题位, 用书法字体（未就绪时回落默认字体）
        var titleFont = ShuimoFont.Title;
        var fontPop = titleFont is { Available: true } ? titleFont.Push() : null;

        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var textSize = ImGui.CalcTextSize(title);

        // 主题色圆点
        var dotCenter = new Vector2(pos.X + 4f, pos.Y + textSize.Y * 0.5f);
        drawList.AddCircleFilled(dotCenter, 3f, SimplePalette.ToU32(SimplePalette.Accent));

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 12f);
        ImGui.PushStyleColor(ImGuiCol.Text, SimplePalette.TextPrimary);
        ImGui.Text(title);
        ImGui.PopStyleColor();

        var lineY = pos.Y + textSize.Y * 0.5f;
        var lineStart = new Vector2(pos.X + 12f + textSize.X + 12f, lineY);
        var lineEnd = new Vector2(pos.X + ImGui.GetContentRegionAvail().X, lineY);
        drawList.AddLine(lineStart, lineEnd, SimplePalette.ToU32(SimplePalette.Border), 1f);

        fontPop?.Dispose();
        ImGui.Spacing();
    }

    // int 行：控件在左、标签在右、隐藏 ± 按钮、值居中。
    public static bool DragInt(string label, ref int value, int min, int max, string? tooltip = null)
    {
        HideDragButtonsPush();
        ImGui.SetNextItemWidth(SettingRow.InputWidth);
        var changed = ImGui.DragInt($"##{label}", ref value, 1f, min, max);
        HideDragButtonsPop();
        if (changed) value = Math.Clamp(value, min, max);
        ShowTooltipIfHovered(tooltip);
        DrawTrailingLabel(label);
        return changed;
    }

    // int 行（固定步长）: 拖动速度按 step, 变更后钳制到 [min,max] 并对齐 step 的倍数
    // （Ctrl+点击键入的任意值也会回正）; 控件在左、标签在右。
    public static bool DragIntStep(string label, ref int value, int min, int max, int step, string? tooltip = null)
    {
        HideDragButtonsPush();
        ImGui.SetNextItemWidth(SettingRow.InputWidth);
        var changed = ImGui.DragInt($"##{label}", ref value, step, min, max);
        HideDragButtonsPop();
        if (changed) value = Math.Clamp(value, min, max) / step * step;
        ShowTooltipIfHovered(tooltip);
        DrawTrailingLabel(label);
        return changed;
    }

    // float 行：控件在左、标签在右、隐藏 ± 按钮、值居中。
    public static bool DragFloat(string label, ref float value, float min, float max, string format = "%.1f", string? tooltip = null)
    {
        HideDragButtonsPush();
        ImGui.SetNextItemWidth(SettingRow.InputWidth);
        var changed = ImGui.DragFloat($"##{label}", ref value, 0.1f, min, max, format);
        HideDragButtonsPop();
        if (changed) value = Math.Clamp(value, min, max);
        ShowTooltipIfHovered(tooltip);
        DrawTrailingLabel(label);
        return changed;
    }

    // 枚举行：控件在左、标签在右。
    public static bool Combo(string label, ref int currentIndex, string[] items, string? tooltip = null)
    {
        ImGui.SetNextItemWidth(SettingRow.InputWidth + 40f);
        var changed = ImGui.Combo($"##{label}", ref currentIndex, items, items.Length);
        ShowTooltipIfHovered(tooltip);
        DrawTrailingLabel(label);
        return changed;
    }

    // 字符串行：控件在左、标签在右。
    public static bool InputText(string label, ref string value, uint maxLength = 256, string? tooltip = null)
    {
        ImGui.SetNextItemWidth(SettingRow.InputWidth + 80f);
        var changed = ImGui.InputText($"##{label}", ref value, (int)maxLength);
        ShowTooltipIfHovered(tooltip);
        DrawTrailingLabel(label);
        return changed;
    }

    // 把 Drag 控件的 ± 按钮颜色推为透明；Drag 的值文本天然居中，隐藏按钮后即侧边栏模式数值框形态。
    private static void HideDragButtonsPush()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
    }

    private static void HideDragButtonsPop() => ImGui.PopStyleColor(3);

    private static void ShowTooltipIfHovered(string? tooltip)
    {
        if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
    }

    // 控件右侧同行的标签文本。
    private static void DrawTrailingLabel(string label)
    {
        ImGui.SameLine(0f, 8f);
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);
    }
}
