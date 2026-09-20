using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using Nag0mi.Common.Data;
using Nag0mi.Common.Helper;
using Nag0mi.Common.UI;
using Nag0mi.Gunbreaker.Control;
using Nag0mi.Gunbreaker.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.UI.HotKey;

namespace Nag0mi.Gunbreaker;

// 绝枪 → Nag0miUI 框架接入：Configure 注入职业环境，模式/循环配置（GunbreakerProfiles）
// 的装载、切换、落盘与旧版 UI 配置的一次性迁移。
public partial class GunbreakerRotation
{
    private static readonly string[] HotkeyNames =
    [
        "挑衅", "退避", "亲疏自行",
    ];

    // 自定义热键的技能下拉清单（限定范围：极光 / 刚玉之心 / 石之心(低等级形态)）
    private static readonly (uint Id, string Name, ActionType Type)[] CustomHotkeySkills =
    [
        (GunbreakerSkill.极光, "极光", ActionType.OffGcd),
        (GunbreakerSkill.刚玉之心, "刚玉之心", ActionType.OffGcd),
        (GunbreakerSkill.石之心, "石之心", ActionType.OffGcd),
    ];

    // 当前生效的循环配置（OnEnterAcr 装载，OnExitAcr 清空）；供事件处理器等无引用访问点读取。
    internal static GunbreakerProfiles? ActiveProfiles { get; private set; }

    private GunbreakerProfiles? _profiles;
    private GunbreakerConfigStore? _store; // null = 配置不可写（读失败保原文件），本次运行不落盘

    // 设置页顶部的保存错误提示（旧 GunbreakerControl.SaveError 的接替者）
    public string? SaveError { get; private set; }

    private void ConfigureUi()
    {
        Nag0miUIFramework.Configure(
            jobTag: "GNB",
            jobName: "绝枪战士",
            qtAll: GunbreakerQtDefaults.All,
            qtIsMetaKey: _ => false,
            qtIsVisibleInMode: (_, _) => true,   // 全模式注册，显隐由框架面板记忆 + DefaultVisible 出厂默认
            qtDefault: key => GunbreakerQtDefaults.All.TryGetValue(key, out var v) && v,
            qtCascadeRules: new Dictionary<string, (string, bool)[]>(),   // GNB 无联动规则
            hotkeyNames: HotkeyNames,
            buildHotkeys: BuildHotkeys,
            qtTab基础: GunbreakerQtDefaults.All.Keys
                .Select(key => (key, key, QtIconSkill(key))).ToArray(),
            modeCount: 3,
            modeNames: ["日随", "高难", "自定义"],
            author: "Nag0mi",
            qtIconResolver: GunbreakerQtIcons.ForUi,
            qtDefaultVisible: GunbreakerQtDefaults.DefaultVisible,
            extraTabs: [("循环设置", DrawJobSettingsTab), ("更新日志", Nag0miUIChangelog.Draw), ("开发用", DrawDev)],
            cycleMode: CycleMode,
            currentModeLabel: CurrentModeLabel,
            customHotkeySkills: CustomHotkeySkills);
    }

    // QT 设置页的技能 id（用于解锁校验）：游戏原始图标无对应动作，返回 0 跳过校验。
    private static uint QtIconSkill(string key)
    {
        var icon = GunbreakerQtIcons.For(key);
        return icon.IsGameIcon ? 0 : icon.IconId;
    }

    private static void BuildHotkeys(Nag0miUIHotkeyBuilder builder)
    {
        builder.Fixed("挑衅", GunbreakerSkill.挑衅, ActionType.OffGcd, ActionTargetType.Target);
        builder.Execute("退避", new ExecuteLogic(Fire退避), GunbreakerSkill.退避);
        builder.Fixed("亲疏自行", GunbreakerSkill.亲疏自行, ActionType.OffGcd, ActionTargetType.Self);
    }

    // 退避：仅 8 人小队有效；目标为小队内另一个坦克（正常即小队列表2），
    // 只有自己一个坦克时改为血量最多（百分比最高）的存活队友。
    private static void Fire退避()
    {
        var party = PartyHelper.GetParty();
        if (party.Count != 8)
        {
            HintHelper.ShowToast2("退避：仅 8 人小队有效", 2, HintHelper.HintType.Info);
            return;
        }
        var me = Core.Me;
        if (me == null) return;

        var target = party.FirstOrDefault(c => c.EntityId != me.EntityId && !c.IsDead
                && HealerHelper.IsTank((Job)c.ClassJob.RowId))
            ?? party.Where(c => c.EntityId != me.EntityId && !c.IsDead)
                .OrderByDescending(PartyHelper.GetHpPercent)
                .FirstOrDefault();
        if (target == null)
        {
            HintHelper.ShowToast2("退避：没有有效目标", 2, HintHelper.HintType.Info);
            return;
        }
        HotkeyQueueManager.TryEnqueue(new PAction(GunbreakerSkill.退避, ActionType.OffGcd, ActionTargetType.Self)
        {
            NetworkTid = (uint)target.EntityId,
        });
    }

    // ============================================================
    // === 循环配置（GunbreakerProfiles）装载 / 切换 / 落盘 ===
    // ============================================================

    private void LoadProfiles()
    {
        if (_profiles != null) return;
        var store = new GunbreakerConfigStore(
            Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "Nag0mi.Gunbreaker.json"));
        GunbreakerControlConfig Defaults() => GunbreakerProfiles.Create(GunbreakerSettings.Instance).Config;
        try
        {
            _profiles = new GunbreakerProfiles(store.Load(Defaults));
            _store = store;
            if (store.RecoveryBackup != null)
                PluginLog.Warning($"[Nag0mi] 枪刃配置损坏，已备份到 {store.RecoveryBackup}，恢复默认配置。");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _profiles = new GunbreakerProfiles(Defaults());
            _store = null; // 原文件读不出也不备份成功时，本次运行不落盘以保住原文件
            SaveError = "无法读取或备份配置；本次使用临时配置。请检查配置目录权限后重新加载。";
            PluginLog.Error($"[Nag0mi] {SaveError} {ex.Message}");
        }
    }

    private void SaveProfiles()
    {
        if (_profiles == null || _store == null) return;
        try
        {
            _store.Save(_profiles.Config);
            SaveError = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SaveError = "配置保存失败。请检查配置目录权限和剩余空间。";
            PluginLog.Error($"[Nag0mi] 枪刃配置保存失败：{ex.Message}");
        }
    }

    // 把实时 GunbreakerSettings 回收进当前模式 profile，有变化才落盘。
    // 设置页绘制后与 OnExitAcr 调用。
    private void SyncProfiles()
    {
        if (_profiles?.Capture(GunbreakerSettings.Instance) == true)
            SaveProfiles();
    }

    // 控制条「模式」键与设置页模式下拉共用的切换入口。
    internal void ChangeMode(GunbreakerMode mode)
    {
        if (_profiles == null || mode == _profiles.Config.CurrentMode) return;
        _profiles.Switch(mode, GunbreakerSettings.Instance);
        // 框架按模式索引记忆 QT 显隐/默认值并重建 QT
        Nag0miUISettings.Instance.SwitchMode((int)mode);
        SaveProfiles();
    }

    private void CycleMode()
    {
        if (_profiles != null)
            ChangeMode(GunbreakerProfiles.Next(_profiles.Config.CurrentMode));
    }

    private string CurrentModeLabel() => _profiles?.Config.CurrentMode switch
    {
        GunbreakerMode.Normal => "N",
        GunbreakerMode.HighEnd => "H",
        GunbreakerMode.Custom => "C",
        _ => "模",
    };

    // ============================================================
    // === 旧版 UI 配置一次性迁移 → 框架 GNB.json ===
    // ============================================================

    private void MigrateLegacyUi()
    {
        var legacy = _store?.LegacyUi;
        var ui = Nag0miUISettings.Instance;
        if (legacy == null)
        {
            SeedModeDefaults(ui);
            return;
        }
        try
        {
            if (legacy.QtOrder.Count > 0) ui.QtOrder = legacy.QtOrder;
            if (legacy.QtPanelColumns is { } columns) ui.QtPanelColumns = Math.Clamp(columns, 1, 12);
            if (legacy.QtPanelSpacing is { } spacing) ui.QtPanelSpacing = Math.Clamp((int)spacing, 0, 20);
            if (legacy.QtPanelScale is { } scale) ui.QtPanelScalePercent = Math.Clamp((int)(scale * 100), 50, 200);
            ui.QtPanelOrderLocked = legacy.QtPanelLockOrder;
            if (legacy.HotkeyColumns is { } hColumns) ui.HotkeyColumns = Math.Clamp(hColumns, 1, 12);
            if (legacy.HotkeySpacing is { } hSpacing) ui.HotkeySpacing = Math.Clamp((int)hSpacing, 0, 20);
            // 旧热键面板按像素按钮边长存，框架按缩放百分比（基准 45px）存
            if (legacy.HotkeyButtonSize is { } buttonSize)
                ui.HotkeyScalePercent = Math.Clamp((int)(buttonSize / 45f * 100), 50, 200);
            foreach (var (mode, defaults) in legacy.QtDefaultsByMode)
                if (mode >= 0 && mode < 3)
                    ui.QtDefaultsByMode[mode] = defaults;
            if (legacy.WindowSide is { } side)
            {
                ui.控制条吸附侧 = side;
                ui.控制条相对X = legacy.WindowRelativeX;
                ui.控制条相对Y = legacy.WindowRelativeY;
            }
            ui.Save();
            PluginLog.Log("[Nag0mi] 旧版 UI 配置已迁入框架设置（GNB.json）。");
        }
        catch (Exception ex)
        {
            // 迁移失败不阻塞进入 ACR：框架设置保持现状/默认值
            PluginLog.Error($"[Nag0mi] 旧版 UI 配置迁移失败，使用框架默认值：{ex.Message}");
        }
    }

    // 无旧版配置可迁入时，沿用旧版出厂画像：高难模式默认关闭拉怪/减伤/盾姿辅助。
    private static void SeedModeDefaults(Nag0miUISettings ui)
    {
        if (ui.QtDefaultsByMode.Count != 0) return;
        ui.QtDefaultsByMode[(int)GunbreakerMode.HighEnd] = new Dictionary<string, bool>
        {
            [GunbreakerQT.自动拉怪] = false,
            [GunbreakerQT.自动减伤] = false,
            [GunbreakerQT.强制盾姿] = false,
        };
        ui.Save();
    }
}
