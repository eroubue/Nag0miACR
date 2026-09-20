// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using Dalamud.Bindings.ImGui;

namespace Nag0mi.Common.UI;

// 设置行工具：每个方法对应一行设置，自动处理标签、tooltip、值变更回调。
// 行样式由 SidebarSettingRow 承载（配色随 SimplePalette 暗色方案），本类不含其实现。
// 所有控件使用 ImGui 原生，宽度固定，不做卡片。
public static class SettingRow
{
    // 数值控件固定宽度
    public const float InputWidth = 120f;

    // 分节标题（加粗 + 右侧细线）。
    public static void SectionTitle(string title)
    {
        SidebarSettingRow.SectionTitle(title);
    }

    // bool 行：Checkbox。
    public static bool Checkbox(string label, ref bool value, string? tooltip = null)
    {
        var changed = ImGui.Checkbox(label, ref value);
        if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
        return changed;
    }

    // 按钮行。
    public static bool Button(string label, string? tooltip = null)
    {
        var clicked = ImGui.Button(label);
        if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
        return clicked;
    }

    // int 行：DragInt。
    public static bool DragInt(string label, ref int value, int min, int max, string? tooltip = null)
    {
        return SidebarSettingRow.DragInt(label, ref value, min, max, tooltip);
    }

    // int 行（固定步长）: 拖动每次增减 step, 键入值自动对齐 step 的倍数。
    public static bool DragIntStep(string label, ref int value, int min, int max, int step, string? tooltip = null)
    {
        return SidebarSettingRow.DragIntStep(label, ref value, min, max, step, tooltip);
    }

    // float 行：DragFloat。
    public static bool DragFloat(string label, ref float value, float min, float max, string format = "%.1f", string? tooltip = null)
    {
        return SidebarSettingRow.DragFloat(label, ref value, min, max, format, tooltip);
    }

    // 枚举行：Combo。
    public static bool Combo(string label, ref int currentIndex, string[] items, string? tooltip = null)
    {
        return SidebarSettingRow.Combo(label, ref currentIndex, items, tooltip);
    }

    // 字符串行：InputText。
    public static bool InputText(string label, ref string value, uint maxLength = 256, string? tooltip = null)
    {
        return SidebarSettingRow.InputText(label, ref value, maxLength, tooltip);
    }
}
