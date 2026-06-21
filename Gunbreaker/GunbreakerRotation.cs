using System.Numerics;
using Dalamud.Bindings.ImGui;
using ECommons.ExcelServices;
using ECommons.Logging;
using Nag0mi.Gunbreaker.Action.Always;
using Nag0mi.Gunbreaker.Action.Gcd;
using Nag0mi.Gunbreaker.Action.OffGcd;
using Nag0mi.Gunbreaker.Data;
using Nag0mi.Gunbreaker.Opener;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using PromeRotation.Timeline;

namespace Nag0mi.Gunbreaker;

// 这个RotationMetadata需要实现！

[RotationMetadata((uint)Job.GNB, "绝枪战士", "Nag0mi", "1.0",
    ContentScope = AcrContentScope.HighEnd)]
public class GunbreakerRotation : IRotation
{
   
    
    // 创建一个属于该职业的回调
    private readonly IRotationEventHandler _eventHandler = new GunbreakerRotationEventHandler();
    public IRotationEventHandler GetEventHandler() => _eventHandler;
    
    // 管理该职业所有的决策解析器
    private readonly List<IDecisionResolver> _alwaysResolvers = new();
    private readonly List<IDecisionResolver> _gcdResolvers = new();
    private readonly List<IDecisionResolver> _offGcdResolvers = new();
    
    // 实现对外暴露的静态属性
    // Qt列表
    public static IReadOnlyDictionary<string, bool> QtList { get; } = new Dictionary<string, bool>
    {
        ["停手"] = false,
        ["爆发"] = true,
        ["倾泻爆发"] = false,
        ["AOE"] = true,
        ["无情"] = true,
        ["无情不延后"] = true,
        ["子弹连"] = true,
        ["领域"] = true,
        ["音速破"] = true,
        ["优先音速破"] = false,
        ["弓形"] = true,
        ["突进起手"] = true,
        ["血壤"] = true,
        ["爆发击"] = true,
        ["dot"] = true,
        ["狮心连"] = true,
        ["倍攻"] = true,
        ["闪雷弹"] = true,
        ["命运之环"] = true,
        ["仅使用爆发击卸除子弹"] = false,
        ["小于3目标时不用弓形"] = false,
        ["弓形冲波允许错开无情"] = false,
        ["落地无情"] = false,
        ["血壤不延后"] = false,
        ["爆发药"] = true,
        ["优先狮心连"] = false,
        ["自动拉怪"] = true,
        ["自动减伤"] = true,
        ["强制盾姿"] = true,
        ["启用起手"] = true
        
    };
    // 起手列表
    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>
    {
        ["妖星起手"] = typeof(妖星),
        ["无情2g起手"] = typeof(无情2g)
    };
    
    public GunbreakerRotation()
    {
        // 按照优先级从高到低的顺序，注册所有的求解器
        // _offGcdResolvers.Add(new SimpleOffGcd());
        _alwaysResolvers.Add(new 落地无情());
        _alwaysResolvers.Add(new 续剑());
        
        _gcdResolvers.Add(new 倍攻());
        _gcdResolvers.Add(new 子弹连());
        _gcdResolvers.Add(new 音速破());
        _gcdResolvers.Add(new 狮心连());
        _gcdResolvers.Add(new 命运之环());
        _gcdResolvers.Add(new 爆发击());
        _gcdResolvers.Add(new AOEbase());
        _gcdResolvers.Add(new Base123());
        _gcdResolvers.Add(new 闪雷弹());

        
        _offGcdResolvers.Add(new 无情());
        _offGcdResolvers.Add(new 血壤());
        _offGcdResolvers.Add(new 领域());
        _offGcdResolvers.Add(new 弓形冲波());
        // 画QT
        foreach (var kvp in QtList)
            PromeSettings.Instance.AddQt(kvp.Key, kvp.Value);
        // ── HotkeyPanel 示例 ──
        // 1. 创建面板 → 2. AddHotkey → 3. 注册到 HotkeyManager
        // var panel = new HotkeyPanel(columns: 3, buttonSize: 50f, spacing: 6f, title: "WAR - 技能");
        // panel.AddHotkey("重斩", new PAction(31, ActionType.Gcd, ActionTargetType.Target));
        // panel.AddHotkey("狂暴", new PAction(38, ActionType.OffGcd, ActionTargetType.Self));
        // HotkeyManager.Instance.AddHotkeyPanel(panel);
        //
        // var utilPanel = new HotkeyPanel(columns: 2, title: "WAR - 功能");
        // utilPanel.AddHotkey("疾跑", new PAction(3, ActionType.OffGcd, ActionTargetType.Self));
        // utilPanel.AddHotkey("开关", new ToggleLogic(
        //     () => PromeSettings.Instance.GetQt("自动减伤"),
        //     () => PromeSettings.Instance.SetQt("自动减伤",
        //         !PromeSettings.Instance.GetQt("自动减伤"))),
        //     iconActionId: 44);
        // HotkeyManager.Instance.AddHotkeyPanel(utilPanel);
        
    }
    
    // 该职业的起手
    public IOpener? GetOpener()
    {
        if (!PromeSettings.Instance.GetQt(GunbreakerQT.启用起手) || Core.Me == null)
            return null;

        var openerName = "";
        var openerSource = "";

        var settingChoice = GunbreakerSettings.Instance.起手选择;
        switch (settingChoice)
        {
            case GunbreakerSettings.起手选择枚举.妖星起手:
                openerName = "妖星起手";
                openerSource = "Settings";
                break;
            case GunbreakerSettings.起手选择枚举.无情2g起手:
                openerName = "无情2g起手";
                openerSource = "Settings";
                break;
        }

        if (string.IsNullOrWhiteSpace(openerName))
        {
            openerName = PromeRotation.PureTimeline.PtlManager.CurrentOpener;
            openerSource = "PureTimeline";
        }

        if (string.IsNullOrWhiteSpace(openerName))
        {
            var meta = TimelineManager.CurrentMeta;
            openerName = meta?.Opener;
            openerSource = "Timeline";
        }

        if (string.IsNullOrWhiteSpace(openerName))
            return null;

        var openers = RotationManager.GetOpenersByJob((int)Core.Me.ClassJob.RowId);
        if (openers == null || !openers.TryGetValue(openerName, out var openerType))
        {
            PluginLog.Warning($"[ACR] {openerSource} 指定起手不存在：{openerName}");
            return null;
        }

        try
        {
            if (Activator.CreateInstance(openerType) is IOpener opener)
            {
                PluginLog.Information($"[ACR] 从{openerSource} 加载起手：{openerName}");
                return opener;
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error($"[ACR] 创建起手实例失败: {ex.Message}");
        }

        return null;
    }
    public PAction? NextAlways()
    {
        // 遍历所有Alway解析器
        foreach (var resolver in _alwaysResolvers)
        {
            if (resolver.Check().Success)
            {
                // 找到第一个满足条件的，返回它的决策结果
                return resolver.GetAction();
            }
        }
        // 如果所有求解器都不满足条件，返回null
        return null;
    }
    
    public PAction? NextGcd()
    {
        // 遍历所有GCD解析器
        foreach (var resolver in _gcdResolvers)
        {
            if (resolver.Check().Success)
            {
                // 找到第一个满足条件的，返回它的决策结果
                return resolver.GetAction();
            }
        }
        // 如果所有求解器都不满足条件，返回null
        return null;
    }
    
    public PAction? NextOffGcd()
    {
        // 遍历所有oGCD解析器 同上
        foreach (var resolver in _offGcdResolvers)
        {
            if (resolver.Check().Success)
            {
                return resolver.GetAction();
            }
        }
        return null;
    }
    
    public void UpdateDebugStatus()
    {
        // 清空上一帧的旧数据
        RotationManager.AlwaysSolverStatus.Clear();
        RotationManager.GcdSolverStatus.Clear();
        RotationManager.OffGcdSolverStatus.Clear();
        // Always状态列表
        foreach (var resolver in _alwaysResolvers)
        {
            var result = resolver.Check();
            
            RotationManager.AlwaysSolverStatus.Add(new SolverStatus
            {
                Name = resolver.GetType().Name,
                Success = result.Success,
                Message = result.Message
            });
        }
        
        // GCD状态列表
        foreach (var resolver in _gcdResolvers)
        {
            var result = resolver.Check();
            
            RotationManager.GcdSolverStatus.Add(new SolverStatus
            {
                Name = resolver.GetType().Name,
                Success = result.Success,
                Message = result.Message
            });
        }
        
        // OGCD状态列表
        foreach (var resolver in _offGcdResolvers)
        {
            var result = resolver.Check();
            
            RotationManager.OffGcdSolverStatus.Add(new SolverStatus
            {
                Name = resolver.GetType().Name,
                Success = result.Success,
                Message = result.Message
            });
        }
    }

    public void DrawQTs()
    {
        
        
    }

    public void DrawSettings()
    {
        if (ImGui.BeginTabBar("Settings"))
        {
            if (ImGui.BeginTabItem("设置"))
            {
                DrawGeneral();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("开发用"))
            {
                DrawDev();
                ImGui.EndTabItem();
            }
        }
    }

    private void DrawGeneral()
    {
        ImGui.Dummy(new Vector2(0, 5));
        ImGui.TextColored(new Vector4(1f, 0.8f, 0.6f, 1f), "—————— 通用设置 ——————");

        var workMode = (int)GunbreakerSettings.Instance.当前工作模式;
        string[] workModeOptions = { "高难模式", "日常模式" };
        ImGui.SetNextItemWidth(200f);
        if (ImGui.Combo("工作模式", ref workMode, workModeOptions, workModeOptions.Length))
            GunbreakerSettings.Instance.当前工作模式 = (GunbreakerSettings.工作模式枚举)workMode;

        var st = GunbreakerSettings.Instance.ST;
        if (ImGui.Checkbox("单体模式(ST)", ref st))
            GunbreakerSettings.Instance.ST = st;

        var aoeCount = GunbreakerSettings.Instance.AOE数;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderInt("AOE目标数", ref aoeCount, 1, 10))
            GunbreakerSettings.Instance.AOE数 = aoeCount;

        ImGui.Spacing();

        var reservedAmmo = GunbreakerSettings.Instance.保留子弹数;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderInt("保留子弹数", ref reservedAmmo, 0, 3))
            GunbreakerSettings.Instance.保留子弹数 = reservedAmmo;

        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.7f, 1f, 0.7f, 1f), "—————— 起手设置 ——————");

        var openerMode = (int)GunbreakerSettings.Instance.开怪方式;
        string[] openerModeOptions = { "关闭", "突进", "闪雷弹" };
        ImGui.SetNextItemWidth(200f);
        if (ImGui.Combo("开怪方式", ref openerMode, openerModeOptions, openerModeOptions.Length))
            GunbreakerSettings.Instance.开怪方式 = (GunbreakerSettings.起手方式枚举)openerMode;

        var openerAdvance = GunbreakerSettings.Instance.开怪提前时间;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.SliderInt("开怪提前时间(ms)", ref openerAdvance, 0, 3000))
            GunbreakerSettings.Instance.开怪提前时间 = openerAdvance;

        ImGui.Spacing();

        var openerSelect = (int)GunbreakerSettings.Instance.起手选择;
        string[] openerSelectOptions = { "妖星起手", "无情2g起手" };
        ImGui.SetNextItemWidth(200f);
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
        
        ImGui.TextColored(new Vector4(0.5f, 1f, 1f, 1f), "—————— Solver状态 ——————");

        if (RotationManager.GcdSolverStatus.Count > 0)
        {
            if (ImGui.BeginChild("SolverStatus", new Vector2(0, 400), true))
            {
                ImGui.TextColored(new Vector4(1f, 1f, 0.7f, 1f), "Always / Gcd Solver:");
                foreach (var status in RotationManager.GcdSolverStatus)
                {
                    var color = status.Success
                        ? new Vector4(0.3f, 1f, 0.3f, 1f)
                        : new Vector4(0.7f, 0.7f, 0.7f, 1f);
                    ImGui.TextColored(color, $"  [{status.Name}] {(status.Success ? "O" : "X")} {status.Message}");
                }

                ImGui.Spacing();
                ImGui.TextColored(new Vector4(1f, 0.8f, 1f, 1f), "OffGcd Solver:");
                foreach (var status in RotationManager.OffGcdSolverStatus)
                {
                    var color = status.Success
                        ? new Vector4(0.3f, 1f, 0.3f, 1f)
                        : new Vector4(0.7f, 0.7f, 0.7f, 1f);
                    ImGui.TextColored(color, $"  [{status.Name}] {(status.Success ? "O" : "X")} {status.Message}");
                }

                ImGui.EndChild();
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "  等待战斗数据...");
        }

        ImGui.Dummy(new Vector2(0, 5));
    }
    
}
