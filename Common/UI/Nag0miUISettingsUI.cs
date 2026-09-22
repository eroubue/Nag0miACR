// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Nag0mi.Common.Data;
using PromeRotation.Helpers;

namespace Nag0mi.Common.UI;

// 设置面板 UI（多职业共用）。QT 列表/热键显隐经 Nag0miUIJobEnv 读当前职业注入数据；
// 模式泛化为模式索引（0..ModeCount-1）。
public static class Nag0miUISettingsUI
{
    // 界面滑杆行：原生带标签滑杆（宽随调用方）。返回是否刚松手（提交信号, 供调用方落盘）。
    private static bool 主题滑杆Int(string label, ref int value, int min, int max, float width)
    {
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt(label, ref value, min, max);
        return ImGui.IsItemDeactivatedAfterEdit();
    }

    // ============================================================
    // 一键显示/隐藏热键面板与 QT 面板悬浮窗（面板控制页）
    private static void DrawPanelsVisibilityButton()
    {
        var panelsVisible = Nag0miUIFramework.任一面板可见;
        if (SettingRow.Button(panelsVisible ? "隐藏 Hotkey/QT 面板" : "显示 Hotkey/QT 面板",
                "一键显示/隐藏热键面板与 QT 面板悬浮窗"))
            Nag0miUIFramework.SetPanelsVisible(!panelsVisible);
    }

    // 面板位置锁定：开启后对应悬浮面板不可左键拖动（防战斗误移）; 点击开关/排序不受影响
    private static void DrawPanelPositionLocks()
    {
        var s = Nag0miUISettings.Instance;

        var qtLocked = s.Qt面板位置锁定;
        if (SettingRow.Checkbox("锁定QT面板位置", ref qtLocked, "开启后 QT 悬浮面板不可左键拖动换位（防战斗误移）"))
        {
            s.Qt面板位置锁定 = qtLocked;
            s.Save();
        }

        var hotkeyLocked = s.热键面板位置锁定;
        if (SettingRow.Checkbox("锁定热键面板位置", ref hotkeyLocked, "开启后热键悬浮面板不可左键拖动换位（防战斗误移）"))
        {
            s.热键面板位置锁定 = hotkeyLocked;
            s.Save();
        }
    }

    // ============================================================
    // 「面板控制」页：面板显隐/锁定 + QT·热键面板布局 + QT 列表（显隐/默认值）+ 热键显隐
    public static void DrawPanelControl()
    {
        Hdr("面板控制");
        DrawPanelsVisibilityButton();
        DrawPanelPositionLocks();

        var s = Nag0miUISettings.Instance;
        const float sliderWidth = 360f;

        // QT 面板布局：拖动中只改内存值, 面板实时跟随; 松手才落盘
        Hdr("QT 面板");
        int qtCols = s.QtPanelColumns;
        var qtSave = 主题滑杆Int("每行数量", ref qtCols, 1, 6, sliderWidth);
        s.QtPanelColumns = qtCols;

        int qtSpacing = s.QtPanelSpacing;
        qtSave |= 主题滑杆Int("间隔(px)", ref qtSpacing, 0, 20, sliderWidth);
        s.QtPanelSpacing = qtSpacing;

        int qtScale = s.QtPanelScalePercent;
        qtSave |= 主题滑杆Int("缩放(%)", ref qtScale, 50, 200, sliderWidth);
        s.QtPanelScalePercent = qtScale;

        var qtOrderLocked = s.QtPanelOrderLocked;
        if (SettingRow.Checkbox("锁定QT排序", ref qtOrderLocked, "开启后 QT 悬浮面板按钮不可右键拖拽换位（防战斗误拖）"))
        {
            s.QtPanelOrderLocked = qtOrderLocked;
            qtSave = true;
        }
        if (qtSave) s.Save();

        Hdr("QT 列表");
        DrawQtList();

        // 热键面板布局：松手才重建, 避免拖动中每帧建销面板窗口
        Hdr("热键面板");
        var hkRebuild = false;

        int hkCols = s.HotkeyColumns;
        hkRebuild |= 主题滑杆Int("每行数量", ref hkCols, 1, 12, sliderWidth);
        s.HotkeyColumns = hkCols;

        int hkSpacing = s.HotkeySpacing;
        hkRebuild |= 主题滑杆Int("间隔(px)", ref hkSpacing, 0, 20, sliderWidth);
        s.HotkeySpacing = hkSpacing;

        int hkScale = s.HotkeyScalePercent;
        hkRebuild |= 主题滑杆Int("缩放(%)", ref hkScale, 50, 200, sliderWidth);
        s.HotkeyScalePercent = hkScale;

        var hkOrderLocked = s.HotkeyPanelOrderLocked;
        if (SettingRow.Checkbox("锁定热键排序", ref hkOrderLocked, "开启后热键悬浮面板按钮不可右键拖拽换位（防战斗误拖）"))
        {
            s.HotkeyPanelOrderLocked = hkOrderLocked;
            s.Save();
        }

        Hdr("热键显隐");
        foreach (var name in s.GetOrderedHotkeyNames())
        {
            bool shown = !s.HiddenHotkeys.Contains(name);
            if (SettingRow.Checkbox(name, ref shown))
            {
                if (shown) s.HiddenHotkeys.Remove(name);
                else if (!s.HiddenHotkeys.Contains(name)) s.HiddenHotkeys.Add(name);
                s.Save();
                hkRebuild = true;
            }
        }

        if (hkRebuild)
        {
            s.Save();
            Nag0miUIHotkeyUI.Rebuild();
        }
    }

    // QT 列表行图标边长(px)与解析失败回落（游戏内问号图标, 与悬浮面板同口径）
    private const float QtListIconSize = 32f;
    private const uint 问号图标 = 60071u;

    // QT 列表：每行 = QT 图标（悬浮显名）+ 复选框①显示/隐藏 + 复选框②默认值, 均按当前模式。
    // 元键与当前模式不可见的模式专属键不列出; 顺序 = 自定义排序, 与悬浮面板一致。
    private static void DrawQtList()
    {
        var s = Nag0miUISettings.Instance;
        var dict = s.GetCurrentModeDefaults();
        ImGui.TextDisabled("显示 = QT 面板显隐 ｜ 默认 = 默认值开关（均按当前模式）");

        foreach (var key in s.GetOrderedQtKeys())
        {
            if (Nag0miUIJobEnv.QtIsMetaKey(key)) continue;
            if (!Nag0miUIJobEnv.QtIsVisibleInMode(key, s.ModeIndex)) continue;

            ImGui.PushID($"qtrow_{key}");

            var r = Nag0miUIJobEnv.QtIconResolver?.Invoke(key);
            var (iconId, isGameIcon) = r is { iconId: > 0 } ? (r.Value.iconId, r.Value.isGameIcon) : (问号图标, true);
            var tex = isGameIcon ? IconHelper.GetGameIcon(iconId) : IconHelper.GetActionIcon(iconId);
            if (tex != null)
                ImGui.Image(tex.Handle, new Vector2(QtListIconSize));
            else
                ImGui.Dummy(new Vector2(QtListIconSize));   // 贴图未就绪当帧占位, 布局不跳
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(key);

            ImGui.SameLine(0f, 12f);
            var visible = s.IsQtVisible(key);
            if (SettingRow.BareCheckbox("##vis", ref visible, "显示/隐藏（当前模式）"))
                s.SetQtVisible(key, visible);

            ImGui.SameLine(0f, 12f);
            var def = dict.TryGetValue(key, out var dv) ? dv : Nag0miUIJobEnv.QtDefault(key);
            if (SettingRow.BareCheckbox("##def", ref def, "默认值开关（当前模式, 勾选即同步实际 QT）"))
            {
                dict[key] = def;
                APIHelper.设置QT(key, def);   // 显示的就是当前模式: 立即同步 QT 面板实际状态
                s.Save();
            }

            ImGui.PopID();
        }

        if (SettingRow.Button("从当前QT导入", "用 QT 面板当前状态覆盖当前模式默认值"))
        {
            s.SaveQtSnapshot(s.ModeIndex);
            s.Save();
            HintHelper.ShowToast2("已用 QT 面板当前状态覆盖本模式默认值", 3, HintHelper.HintType.Info);
        }
    }

    // ============================================================
    // === 个性化页：主题 / 逐窗口字体 / 背景图（全职业共用, 存 Common.json） ===
    // ============================================================
    public static void DrawPersonalization()
    {
        var c = Nag0miUICommonSettings.Instance;
        void Save() => c.Save();

        // 水墨主题明/暗：改即落盘并写入 ShuimoPalette.DarkMode，色板每帧读取、下一帧全窗口生效
        Hdr("主题");
        var dark = c.DarkMode;
        if (SettingRow.Checkbox("暗色模式（暖宣）", ref dark, "明 = 冷宣纸底，暗 = 黑底暖宣"))
        {
            c.DarkMode = dark;
            ShuimoPalette.DarkMode = dark;
            c.Save();
        }

        Hdr("窗口字体");
        WindowFontSettingsUI.DrawAllWindowFontSettings(
            c.设置窗口字体, c.Qt面板字体, c.热键面板字体, Save);

        Hdr("窗口背景");
        WindowBackgroundSettingsUI.DrawAllWindowBackgroundSettings(
            c.设置窗口背景, c.Qt面板背景, c.热键面板背景, Save);
    }

    // ============================================================
    // === 热键自定义页：仅自定义热键节（显隐/布局在「面板控制」页） ===
    // ============================================================
    public static void DrawHotkeyCustom()
    {
        var s = Nag0miUISettings.Instance;
        DrawCustomHotkeys(s);
    }

    // ============================================================
    // === 自定义热键（技能+目标 双下拉 + 添加/删除） ===
    // ============================================================

    // 下拉选中索引（会话内记忆, 不落盘）
    private static int _自定义技能索引;
    private static int _自定义目标索引;

    // 自定义热键管理小节：职业未注入技能清单时不显示。
    private static void DrawCustomHotkeys(Nag0miUISettings s)
    {
        var skills = Nag0miUIJobEnv.CustomHotkeySkills;
        if (skills.Length == 0) return;

        Hdr("自定义热键");

        var skillNames = skills.Select(k => k.Name).ToArray();
        var targets = CustomHotkeyTargets.Selectable;
        var targetNames = targets.Select(CustomHotkeyTargets.Label).ToArray();
        _自定义技能索引 = Math.Clamp(_自定义技能索引, 0, skillNames.Length - 1);
        _自定义目标索引 = Math.Clamp(_自定义目标索引, 0, targets.Length - 1);
        SettingRow.Combo("自定义技能", ref _自定义技能索引, skillNames);
        SettingRow.Combo("自定义目标", ref _自定义目标索引, targetNames);

        if (SettingRow.Button("添加自定义热键", "按当前下拉选择生成一个自定义热键按钮"))
        {
            var skill = skills[_自定义技能索引];
            var target = targets[_自定义目标索引];
            var name = CustomHotkeyTargets.GenerateUniqueName(
                $"{skill.Name}·{CustomHotkeyTargets.Label(target)}", s.GetOrderedHotkeyNames());
            s.CustomHotkeys.Add(new CustomHotkeyEntry
            {
                Name = name,
                SkillId = skill.Id,
                Type = skill.Type,
                Target = target,
            });
            s.Save();
            Nag0miUIHotkeyUI.Rebuild();
        }

        // 已有条目：删除时把名字从显隐/排序记录一并清除
        for (var i = 0; i < s.CustomHotkeys.Count; i++)
        {
            var entry = s.CustomHotkeys[i];
            ImGui.PushID($"##customhk{i}");
            if (ImGui.SmallButton("删除"))
            {
                s.CustomHotkeys.RemoveAt(i);
                s.HiddenHotkeys.Remove(entry.Name);
                s.HotkeyOrder.Remove(entry.Name);
                s.Save();
                Nag0miUIHotkeyUI.Rebuild();
                ImGui.PopID();
                break;
            }
            ImGui.SameLine(0f, 8f);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(entry.Name);
            ImGui.PopID();
        }
    }

    // 分节标题（主题色圆点 + 主文字 + 尾随细线）
    private static void Hdr(string t) => SettingRow.SectionTitle(t);
}
