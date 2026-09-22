using ECommons.ExcelServices;
using ECommons.Logging;
using Nag0mi.Common.Data;
using Nag0mi.Common.UI;
using Nag0mi.Gunbreaker.Action.Always;
using Nag0mi.Gunbreaker.Action.Gcd;
using Nag0mi.Gunbreaker.Action.OffGcd;
using Nag0mi.Gunbreaker.Data;
using Nag0mi.Gunbreaker.Opener;
using Nag0mi.Gunbreaker.Timeline;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using PromeRotation.Timeline;
using PromeRotation.Timeline.Core;

namespace Nag0mi.Gunbreaker;

// 这个RotationMetadata需要实现！

[RotationMetadata((uint)Job.GNB, "绝枪战士", "Nag0mi", "1.7",
    ContentScope = AcrContentScope.HighEnd)]
public partial class GunbreakerRotation : IRotation, IRotationLifecycle, IDisposable
{
    public void OnEnterAcr()
    {
        LoadProfiles();
        if (_profiles != null)
        {
            _profiles.Apply(GunbreakerSettings.Instance);
            MigrateLegacyUi();
            // 框架按模式索引记忆 QT 显隐/默认值：与循环配置的当前模式对齐
            var ui = Nag0miUISettings.Instance;
            var modeIndex = (int)_profiles.Config.CurrentMode;
            if (ui.ModeIndex != modeIndex) ui.SwitchMode(modeIndex);
            else ui.RestoreQtSnapshot(modeIndex);
        }
        Nag0miUIFramework.Install();
        ActiveProfiles = _profiles;
    }

    public void OnExitAcr()
    {
        ActiveProfiles = null;
        SyncProfiles();
        Nag0miUIFramework.Uninstall();
        _profiles = null;
        _store = null;
    }

    public void Dispose() => OnExitAcr();

    // 创建一个属于该职业的回调
    private readonly IRotationEventHandler _eventHandler = new GunbreakerRotationEventHandler();
    public IRotationEventHandler GetEventHandler() => _eventHandler;
    
    // 管理该职业所有的决策解析器
    private readonly List<IDecisionResolver> _alwaysResolvers = new();
    private readonly List<IDecisionResolver> _gcdResolvers = new();
    private readonly List<IDecisionResolver> _offGcdResolvers = new();
    
    // 实现对外暴露的静态属性
    // Qt列表
    public static IReadOnlyDictionary<string, bool> QtList => GunbreakerQtDefaults.All;

    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>
    {
        ["妖星起手"] = typeof(妖星),
        ["无情2g起手"] = typeof(无情2g),
        ["绝欧起手"] = typeof(绝欧),
        ["龙诗起手"] = typeof(龙诗),
        ["绝亚起手"] = typeof(绝亚),
        ["神兵起手"] = typeof(神兵),
        ["巴哈起手"] = typeof(巴哈)
    };

    // 时间轴职业专属条件 / 行为节点
    public static IJobNodeProvider NodeProvider { get; } = new GunbreakerJobNodeProvider();
    
    public GunbreakerRotation()
    {
        // 向 Nag0miUI 框架注入本职业全部环境（QT 表、热键、模式、图标解析、设置页注入）
        ConfigureUi();
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
        // QT 注册与热键面板由框架在 OnEnterAcr → Nag0miUIFramework.Install() 时统一建立
    }
    
    // 该职业的起手
    public IOpener? GetOpener()
    {
        if (Core.Me == null)
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
            case GunbreakerSettings.起手选择枚举.绝欧起手:
                openerName = "绝欧起手";
                openerSource = "Settings";
                break;
            case GunbreakerSettings.起手选择枚举.龙诗起手:
                openerName = "龙诗起手";
                openerSource = "Settings";
                break;
            case GunbreakerSettings.起手选择枚举.绝亚起手:
                openerName = "绝亚起手";
                openerSource = "Settings";
                break;
            case GunbreakerSettings.起手选择枚举.神兵起手:
                openerName = "神兵起手";
                openerSource = "Settings";
                break;
            case GunbreakerSettings.起手选择枚举.巴哈起手:
                openerName = "巴哈起手";
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

}
