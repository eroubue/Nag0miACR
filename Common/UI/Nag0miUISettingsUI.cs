// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Nag0mi.Common.Data;
using PromeRotation.Helpers;

namespace Nag0mi.Common.UI;

// 设置面板 UI（多职业共用）。
// QT面板页经 Nag0miUIJobEnv 读当前职业 QT 表；模式泛化为模式索引（0..ModeCount-1）。
public static class Nag0miUISettingsUI
{
    // ============================================================
    // === 模式切换行（基础设置 / QT面板 各页共用） ===
    // ============================================================

    // 当前模式显示名（下标越界或未注入时退化为「模式N」）
    private static string ModeName(int index)
    {
        var names = Nag0miUIJobEnv.ModeNames;
        return index >= 0 && index < names.Length ? names[index] : $"模式{index}";
    }

    // 模式切换行：模式色「当前模式：xx」+ 同排胶囊切换按钮（循环切到下一模式）。
    public static void DrawModeSwitchRow()
    {
        var s = Nag0miUISettings.Instance;
        var index = s.ModeIndex;
        var next = (index + 1) % Nag0miUIJobEnv.ModeCount;

        ImGui.AlignTextToFramePadding();   // 文字与 28 高胶囊垂直居中
        ImGui.PushStyleColor(ImGuiCol.Text,
            index == 0 ? SimplePalette.ModeDailyText : SimplePalette.ModeHighEndText);
        ImGui.Text($"当前模式：{ModeName(index)}");
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 10f);
        if (ModeCapsule($"切换{ModeName(next)}"))
        {
            HintHelper.ShowToast2($"切换至{ModeName(next)}模式", 2, HintHelper.HintType.Info);
            s.SwitchMode(next);
        }
    }

    // 模式胶囊按钮：透明底 + 悬停淡染，宽随文字、高 28；文字用主文字色（与其他按钮一致）。
    private static bool ModeCapsule(string label)
    {
        var style = ImGui.GetStyle();
        var w = ImGui.CalcTextSize(label).X + style.FramePadding.X * 2f;
        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SimplePalette.FrameBgHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, SimplePalette.FrameBgActive);
        ImGui.PushStyleColor(ImGuiCol.Text, SimplePalette.TextPrimary);
        var clicked = ImGui.Button(label, new Vector2(w, 28f));
        ImGui.PopStyleColor(4);
        return clicked;
    }

    // 界面滑杆行：原生带标签滑杆（宽随调用方）。返回是否刚松手（提交信号, 供调用方落盘）。
    private static bool 主题滑杆Int(string label, ref int value, int min, int max, float width)
    {
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt(label, ref value, min, max);
        return ImGui.IsItemDeactivatedAfterEdit();
    }

    // ============================================================
    // 一键显示/隐藏热键面板与 QT 面板悬浮窗（基础设置页）
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
    // 「基础设置」分组下的两个子栏：面板控制 / 基础设置
    public static void DrawPanelControl()
    {
        Hdr("面板控制");
        DrawPanelsVisibilityButton();
        DrawPanelPositionLocks();
    }

    public static void DrawBasicSettings()
    {
        Hdr("基础设置");
        DrawModeSwitchRow();
        DrawDarkModeRow();
    }

    // 水墨主题明/暗切换：改即落盘并写入 ShuimoPalette.DarkMode，色板每帧读取、下一帧全窗口生效
    private static void DrawDarkModeRow()
    {
        var c = Nag0miUICommonSettings.Instance;
        var dark = c.DarkMode;
        if (SettingRow.Checkbox("暗色模式（暖宣）", ref dark, "切换水墨主题明/暗：明=冷宣纸底，暗=黑底暖宣"))
        {
            c.DarkMode = dark;
            ShuimoPalette.DarkMode = dark;
            c.Save();
        }
    }

    // ============================================================
    // === 个性化页：逐窗口字体 / 背景图（全职业共用, 存 Common.json） ===
    // ============================================================
    public static void DrawPersonalization()
    {
        var c = Nag0miUICommonSettings.Instance;
        void Save() => c.Save();

        Hdr("窗口字体");
        WindowFontSettingsUI.DrawAllWindowFontSettings(
            c.设置窗口字体, c.Qt面板字体, c.热键面板字体, Save);

        Hdr("窗口背景");
        WindowBackgroundSettingsUI.DrawAllWindowBackgroundSettings(
            c.设置窗口背景, c.Qt面板背景, c.热键面板背景, Save);
    }

    // ============================================================
    // === QT面板页：顶部按钮在「QT显隐 / QT默认值」两个子视图间切换 ===
    // ============================================================

    // QT面板页子视图（显隐管理 / 默认值管理）；会话内记住上次选择，不写入配置文件。
    private enum QtSubview
    {
        显隐,
        默认值,
    }

    private static QtSubview _qtSubview = QtSubview.显隐;

    // QT面板页：顶部切换按钮行 + 按所选子视图绘制显隐管理 / 默认值管理。
    public static void DrawQtPanel()
    {
        QtSubviewButton("QT显隐", QtSubview.显隐);
        ImGui.SameLine(0f, 8f);
        QtSubviewButton("QT默认值", QtSubview.默认值);

        ImGui.Separator();

        if (_qtSubview == QtSubview.显隐) DrawQtManage();
        else DrawDefaultsManage();
    }

    // 子视图切换按钮：激活项实底高亮 + 主文字色，未激活透明底 + 次级文字色；宽随文字、高 28。
    private static void QtSubviewButton(string label, QtSubview view)
    {
        var isActive = _qtSubview == view;
        if (isActive)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, SimplePalette.NavActiveBg);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SimplePalette.WithAlpha(SimplePalette.Accent, 0.25f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, SimplePalette.WithAlpha(SimplePalette.Accent, 0.35f));
            ImGui.PushStyleColor(ImGuiCol.Text, SimplePalette.NavActiveText);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SimplePalette.FrameBgHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, SimplePalette.FrameBgActive);
            ImGui.PushStyleColor(ImGuiCol.Text, SimplePalette.TextSecondary);
        }

        var style = ImGui.GetStyle();
        var w = ImGui.CalcTextSize(label).X + style.FramePadding.X * 2f;
        if (ImGui.Button(label, new Vector2(w, 28f)))
            _qtSubview = view;

        ImGui.PopStyleColor(4);
    }

    // ============================================================
    private static void DrawQtManage()
    {
        Hdr("QT 按钮显隐管理");
        ImGui.TextWrapped("勾选 = 在 QT 面板显示该开关按钮；取消勾选 = 从 QT 面板隐藏。");
        ImGui.TextWrapped("显隐配置按模式各存一套，改动只作用于当前模式；切换模式自动套用该模式的保存记录。");
        ImGui.TextWrapped("模式专属开关只在所属模式的列表中显示。");
        ImGui.TextWrapped("只控制显隐，不改变开关的当前状态。隐藏后QT仍按默认值生效。");
        DrawModeSwitchRow();

        // QT 面板布局调整（同 Hotkey 页滑块模式; 面板每帧按设置重排, 改动松手即存即生效）
        Hdr("QT 面板");
        ImGui.TextWrapped("QT 悬浮面板：布局改动松手后自动保存并即时生效。");
        var s = Nag0miUISettings.Instance;
        var sliderWidth = 360f;

        int cols = s.QtPanelColumns;
        var colsSaved = 主题滑杆Int("每行数量", ref cols, 1, 6, sliderWidth);
        s.QtPanelColumns = cols;          // 拖动中只改内存值, 面板实时跟随
        bool save = colsSaved;            // 松手才落盘

        int spacing = s.QtPanelSpacing;
        save |= 主题滑杆Int("间隔(px)", ref spacing, 0, 20, sliderWidth);
        s.QtPanelSpacing = spacing;

        int scale = s.QtPanelScalePercent;
        save |= 主题滑杆Int("缩放(%)", ref scale, 50, 200, sliderWidth);
        s.QtPanelScalePercent = scale;

        if (save) s.Save();

        // QT 排序模块: 标题 + 排序说明 + 锁定开关
        Hdr("QT 排序");
        ImGui.TextWrapped("QT 面板按钮的排列顺序在悬浮面板上按住鼠标右键拖动调整，改动即时生效并与本页顺序同步。");
        var locked = s.QtPanelOrderLocked;
        if (SettingRow.Checkbox("锁定QT排序", ref locked, "开启后 QT 悬浮面板按钮不可右键拖拽换位（防战斗误拖）"))
        {
            s.QtPanelOrderLocked = locked;
            s.Save();
        }

        ImGui.Separator();   // 排序模块与下方显隐列表的分界线

        // 全部 QT 平铺一列: 显隐勾选（排列顺序在悬浮面板右键拖动调整, 各页同序）
        DrawQtVisibleList();
    }

    // QT 按钮显隐勾选列表（顺序 = Nag0miUISettings.QtOrder 自定义序, 与悬浮面板一致; 只绑显隐, 不绑开关状态）。
    // 模式专属开关在另一模式的面板不会注册, 勾选了也不显示, 直接不列出（与 QT面板页默认值子视图同口径）。
    private static void DrawQtVisibleList()
    {
        var s = Nag0miUISettings.Instance;
        foreach (var key in s.GetOrderedQtKeys())
        {
            if (Nag0miUIJobEnv.QtIsMetaKey(key)) continue;
            if (!Nag0miUIJobEnv.QtIsVisibleInMode(key, s.ModeIndex)) continue;

            var v = s.IsQtVisible(key);
            if (SettingRow.Checkbox(key, ref v))
                s.SetQtVisible(key, v);
        }
    }

    // ============================================================
    public static void DrawHotkey()
    {
        var s = Nag0miUISettings.Instance;

        Hdr("热键面板");
        ImGui.TextWrapped("手动热键悬浮窗：布局改动松手后或显隐勾选后自动重建面板并保存。");
        bool rebuild = false;

        // 三个滑块统一为 360px，避免滑块过长或过短
        var sliderWidth = 360f;

        int columns = s.HotkeyColumns;
        rebuild |= 主题滑杆Int("每行数量", ref columns, 1, 12, sliderWidth);
        s.HotkeyColumns = columns;                      // 拖动中只改内存值
        // 松手才重建，避免拖动中每帧建销面板窗口

        int spacing = s.HotkeySpacing;
        rebuild |= 主题滑杆Int("间隔(px)", ref spacing, 0, 20, sliderWidth);
        s.HotkeySpacing = spacing;

        int scale = s.HotkeyScalePercent;
        rebuild |= 主题滑杆Int("缩放(%)", ref scale, 50, 200, sliderWidth);
        s.HotkeyScalePercent = scale;

        // 热键排序模块: 标题 + 排序说明 + 锁定开关（与 QT 排序模块同一套交互）
        Hdr("热键排序");
        ImGui.TextWrapped("热键面板按钮的排列顺序在悬浮面板上按住鼠标右键拖动调整，改动即时生效并与本页顺序同步。");
        var locked = s.HotkeyPanelOrderLocked;
        if (SettingRow.Checkbox("锁定热键排序", ref locked, "开启后热键悬浮面板按钮不可右键拖拽换位（防战斗误拖）"))
        {
            s.HotkeyPanelOrderLocked = locked;
            s.Save();
        }

        DrawCustomHotkeys(s);

        ImGui.Separator();

        ImGui.TextWrapped("勾选 = 在热键面板显示该按钮；取消勾选 = 从热键面板隐藏。");
        // 全部热键平铺一列: 显隐勾选（排列顺序在悬浮面板右键拖动调整, 各页同序）
        foreach (var name in s.GetOrderedHotkeyNames())
        {
            bool shown = !s.HiddenHotkeys.Contains(name);
            if (SettingRow.Checkbox(name, ref shown))
            {
                if (shown) s.HiddenHotkeys.Remove(name);
                else if (!s.HiddenHotkeys.Contains(name)) s.HiddenHotkeys.Add(name);
                s.Save();
                rebuild = true;
            }
        }

        if (rebuild)
        {
            s.Save();
            Nag0miUIHotkeyUI.Rebuild();
        }
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
        ImGui.TextWrapped("选择技能与目标后点击添加，生成一个自定义热键按钮；显隐与排序与内置热键相同。");

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

    private static void DrawDefaultsManage()
    {
        Hdr("默认值管理");
        ImGui.TextWrapped("勾选框直接修改当前模式的 QT 默认值，改动即时生效并自动保存；切换模式自动切换到对应模式的默认值。");
        DrawModeSwitchRow();   // 切到哪个模式就显示哪个模式的默认值

        ImGui.Separator();

        // 当前模式的默认值平铺一列（单列跟随当前模式; 顺序 = 自定义排序, 与悬浮面板一致）
        var s = Nag0miUISettings.Instance;
        var dict = s.GetCurrentModeDefaults();
        foreach (var key in s.GetOrderedQtKeys())
        {
            if (Nag0miUIJobEnv.QtIsMetaKey(key)) continue;
            if (!Nag0miUIJobEnv.QtIsVisibleInMode(key, s.ModeIndex)) continue;   // 当前模式下隐藏的 QT 不显示

            var v = dict.TryGetValue(key, out var dv) ? dv : Nag0miUIJobEnv.QtDefault(key);
            if (SettingRow.Checkbox(key, ref v))
            {
                dict[key] = v;
                APIHelper.设置QT(key, v);   // 显示的就是当前模式: 立即同步 QT 面板实际状态
                s.Save();
            }
        }

        ImGui.Separator();

        if (SettingRow.Button("从当前QT导入"))
        {
            s.SaveQtSnapshot(s.ModeIndex);
            s.Save();
            HintHelper.ShowToast2("已用 QT 面板当前状态覆盖本模式默认值", 3, HintHelper.HintType.Info);
        }
    }

    // 分节标题（主题色圆点 + 主文字 + 尾随细线）
    private static void Hdr(string t) => SettingRow.SectionTitle(t);
}
