using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Nag0mi.Common.UI;
using Nag0mi.Gunbreaker.Control;
using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Helpers;

namespace Nag0mi.Gunbreaker;

public partial class GunbreakerRotation
{
    public void DrawSettings() => DrawSettingsContent();

    // 宿主内嵌设置页：框架设置窗入口 + 循环配置（模式/参数）。QT 显隐、面板布局、
    // QT 默认值等由框架设置窗（基础设置/Hotkey/QT面板 + 注入页签）覆盖。
    private void DrawSettingsContent()
    {
        ImGui.PushID("Nag0mi.Gunbreaker.SettingsContent");
        try
        {
            Nag0miUIFramework.DrawSettingsEntry();
            if (SaveError is { } error)
                ImGui.TextWrapped(error);

            if (ImGui.BeginTabBar("Settings"))
            {
                try
                {
                    if (ImGui.BeginTabItem("设置"))
                    {
                        try { DrawGeneral(); }
                        finally { ImGui.EndTabItem(); }
                    }
                    if (ImGui.BeginTabItem("更新日志"))
                    {
                        try { Nag0miUIChangelog.Draw(); }
                        finally { ImGui.EndTabItem(); }
                    }
                    if (ImGui.BeginTabItem("开发用"))
                    {
                        try { DrawDev(); }
                        finally { ImGui.EndTabItem(); }
                    }
                }
                finally { ImGui.EndTabBar(); }
            }
        }
        finally
        {
            ImGui.PopID();
            SyncProfiles();
        }
    }

    // 框架设置窗的「循环设置」注入页（extraTabs），内容与宿主「设置」页一致。
    private void DrawJobSettingsTab()
    {
        DrawGeneral();
        SyncProfiles();
    }

    private void DrawGeneral()
    {
        ImGui.Dummy(new Vector2(0, 5));

        if (_profiles is { } profiles)
        {
            var mode = (int)profiles.Config.CurrentMode;
            string[] modes = ["日随（Normal）", "高难（HighEnd）", "自定义（Custom）"];
            ImGui.SetNextItemWidth(220 * ImGuiHelpers.GlobalScale);
            if (ImGui.Combo("当前配置", ref mode, modes, modes.Length))
                ChangeMode((GunbreakerMode)mode);
            ImGui.TextDisabled("当前配置的修改会自动保存；三个模式可分别编辑。");

            var resetOnDeath = profiles.Config.ResetQtOnDeath;
            if (ImGui.Checkbox("死亡后自动重置当前模式 QT 为默认值", ref resetOnDeath))
            {
                profiles.Config.ResetQtOnDeath = resetOnDeath;
                SaveProfiles();
            }
            ImGui.Separator();
        }

        ImGui.TextColored(new Vector4(1f, 0.8f, 0.6f, 1f), "—————— 通用设置 ——————");

        var workMode = (int)GunbreakerSettings.Instance.当前工作模式;
        string[] workModeOptions = ["高难模式", "日常模式"];
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("辅助策略", ref workMode, workModeOptions, workModeOptions.Length))
            GunbreakerSettings.Instance.当前工作模式 = (GunbreakerSettings.工作模式枚举)workMode;

        var st = GunbreakerSettings.Instance.ST;
        if (ImGui.Checkbox("单体模式(ST)", ref st))
            GunbreakerSettings.Instance.ST = st;

        ImGui.Spacing();

        var reservedAmmo = GunbreakerSettings.Instance.保留子弹数;
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderInt("保留子弹数", ref reservedAmmo, 0, 3, flags: ImGuiSliderFlags.AlwaysClamp))
            GunbreakerSettings.Instance.保留子弹数 = reservedAmmo;

        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.7f, 1f, 0.7f, 1f), "—————— 起手设置 ——————");

        var openerMode = (int)GunbreakerSettings.Instance.开怪方式;
        string[] openerModeOptions = ["关闭", "突进", "闪雷弹"];
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("开怪方式", ref openerMode, openerModeOptions, openerModeOptions.Length))
            GunbreakerSettings.Instance.开怪方式 = (GunbreakerSettings.起手方式枚举)openerMode;

        var openerAdvance = GunbreakerSettings.Instance.开怪提前时间;
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderInt("开怪提前时间(ms)", ref openerAdvance, 0, 3000, flags: ImGuiSliderFlags.AlwaysClamp))
            GunbreakerSettings.Instance.开怪提前时间 = openerAdvance;

        ImGui.Spacing();

        var dashOpener = GunbreakerSettings.Instance.突进起手;
        if (ImGui.Checkbox("突进起手", ref dashOpener))
            GunbreakerSettings.Instance.突进起手 = dashOpener;
        ImGui.TextDisabled("取消勾选后，倒计时 0.3s 改用闪雷弹开怪");

        ImGui.Spacing();

        var openerSelect = (int)GunbreakerSettings.Instance.起手选择;
        string[] openerSelectOptions = ["妖星起手", "无情2g起手", "绝欧起手", "龙诗起手", "绝亚起手", "神兵起手", "巴哈起手"];
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("起手选择", ref openerSelect, openerSelectOptions, openerSelectOptions.Length))
            GunbreakerSettings.Instance.起手选择 = (GunbreakerSettings.起手选择枚举)openerSelect;

        ImGui.Dummy(new Vector2(0, 5));
    }

    private void DrawDev()
    {
        ImGui.Dummy(new Vector2(0, 5));

        var debug = GunbreakerSettings.Instance.debug;
        if (ImGui.Checkbox("Debug模式", ref debug))
            GunbreakerSettings.Instance.debug = debug;

        ImGui.Separator();
        ImGui.TextUnformatted($"子弹连充能: {ActionHelper.GetActionCharges(GunbreakerSkill.烈牙)}");
        ImGui.TextUnformatted($"子弹连CD: {ActionHelper.GetActionCooldown(ActionHelper.GetAdjustedActionId(GunbreakerSkill.烈牙))}");

        if (Core.Me is { } me)
        {
            var level = me.Level;
            ImGui.TextUnformatted($"AOE平衡点(Lv{level}): 基础连{Fmt(GunbreakerAoe.BasicComboBreakpoint(level))} " +
                $"命运之环{Fmt(GunbreakerAoe.FatedCircleBreakpoint(level))} " +
                $"子弹连{Fmt(GunbreakerAoe.GnashingFangBreakpoint(level))} " +
                $"音速破{Fmt(GunbreakerAoe.SonicBreakBreakpoint(level))}");
            var rawCount = (int)TargetHelper.EnemyInRange(5);
            var dying = GunbreakerAoeTracker.CountDyingEnemies(2 * ActionHelper.GetGcdTotal());
            ImGui.TextUnformatted($"5m敌数: {rawCount}（将死{dying}，有效{Math.Max(0, rawCount - dying)}）");
        }

        ImGui.Dummy(new Vector2(0, 5));
    }

    private static string Fmt(int breakpoint) => breakpoint == int.MaxValue ? "-" : breakpoint.ToString();
}
