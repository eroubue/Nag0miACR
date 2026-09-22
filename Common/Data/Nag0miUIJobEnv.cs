// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using Nag0mi.Common.Helper;

namespace Nag0mi.Common.Data;

// 职业环境数据（框架内部使用）。对外注入入口是 Nag0miUIFramework.Configure。
// 在 Rotation 构造函数最前面调用 Configure 注入职业身份与数据源，框架其余部分
// 只读本类、不接触任何职业数据。宿主每次切换职业都会重新构造 Rotation 实例，
// 所以构造时注入即可保证框架看到的永远是当前职业。
internal static class Nag0miUIJobEnv
{
    /// <summary>职业短名（如 GNB），用作窗口名与配置文件名后缀。</summary>
    public static string JobTag { get; private set; } = "GNB";

    /// <summary>职业中文名，用于设置窗口标题。</summary>
    public static string JobName { get; private set; } = "绝枪战士";

    /// <summary>作者身份，决定配置目录、窗口标题前缀与日志前缀。</summary>
    /// <remarks>固定为 "Nag0mi"（配置目录 ACRConfig\Nag0mi\），
    /// 与 RotationMetadata 的 Author 参数保持一致。</remarks>
    public static string 作者 { get; private set; } = "Nag0mi";

    /// <summary>模式数量（如 Normal/HighEnd/Custom = 3），由 Configure 注入，缺省 2。</summary>
    public static int ModeCount { get; private set; } = 2;

    /// <summary>各模式显示名（下标 = 模式索引），由 Configure 注入，缺省为空。</summary>
    public static string[] ModeNames { get; private set; } = Array.Empty<string>();

    /// <summary>QT 开关表：键 → 默认值。由使用方注入，缺省为空。</summary>
    public static IReadOnlyDictionary<string, bool> QtAll { get; private set; } = new Dictionary<string, bool>();

    /// <summary>判断某键是否为元键（元键不进 QT 面板）。</summary>
    public static Func<string, bool> QtIsMetaKey { get; private set; } = _ => false;

    /// <summary>判断某键在指定模式索引下是否可见。</summary>
    public static Func<string, int, bool> QtIsVisibleInMode { get; private set; } = (_, _) => true;

    /// <summary>某键在指定模式索引下的出厂默认显隐（用户未配置显隐时回落到此；缺省全显示）。</summary>
    public static Func<string, int, bool> QtDefaultVisible { get; private set; } = (_, _) => true;

    /// <summary>读取某键的默认值。</summary>
    public static Func<string, bool> QtDefault { get; private set; } = _ => false;

    /// <summary>QT 联动表：写入某键时，一并写入哪些键（invert 为 true 时取反）。</summary>
    public static IReadOnlyDictionary<string, (string key, bool invert)[]> QtCascadeRules { get; private set; }
        = new Dictionary<string, (string, bool)[]>();

    /// <summary>热键面板的按钮名全集，面板控制页热键显隐列表以此为准。</summary>
    public static string[] HotkeyNames { get; private set; } = Array.Empty<string>();

    /// <summary>自定义热键的技能下拉清单（技能id, 显示名, 类型），由使用方注入；为空时设置页不显示自定义热键小节。</summary>
    public static (uint Id, string Name, PromeRotation.Data.ActionType Type)[] CustomHotkeySkills { get; private set; }
        = Array.Empty<(uint, string, PromeRotation.Data.ActionType)>();

    /// <summary>热键「激活中」金框映射：技能id → (buffId, 是否仅看自身)。
    /// 对应 buff 未结束时，热键面板给该格盖 activeaction 贴图框；目标型 buff 看自己+任意队友。</summary>
    public static IReadOnlyDictionary<uint, (uint BuffId, bool SelfOnly)> HotkeyActiveBuffs { get; private set; }
        = new Dictionary<uint, (uint, bool)>();

    /// <summary>热键面板条目构建委托。框架不预置任何按钮，全部条目由使用方在此注册。</summary>
    public static System.Action<Nag0miUIHotkeyBuilder>? BuildHotkeys { get; private set; }

    /// <summary>QT 图标解析器：QT键 → (图标id, 是否游戏原始图标, marker角标文字)。
    /// 返回 null 或 iconId==0 时面板走退化显示（问号图标 + QT 显示名首字角标）。</summary>
    public static Func<string, (uint iconId, bool isGameIcon, string? marker)>? QtIconResolver { get; private set; }

    /// <summary>设置窗口的额外页签（追加在固定页 面板控制/个性化/循环设置/热键自定义 之后），由使用方注入。</summary>
    public static (string label, System.Action draw)[] ExtraTabs { get; private set; }
        = Array.Empty<(string, System.Action)>();

    /// <summary>设置窗口「循环设置」固定页签（基础设置分组子栏）的绘制委托，由使用方注入；未注入时该页空白。</summary>
    public static System.Action? CycleSettingsDraw { get; private set; }

    /// <summary>控制条「模式」键点击回调（切到下一模式），由使用方注入；未注入时该键不响应。</summary>
    public static System.Action? CycleMode { get; private set; }

    /// <summary>控制条「模式」键文字（如 N/H/C），由使用方注入；未注入时显示「模」。</summary>
    public static Func<string>? CurrentModeLabel { get; private set; }

    /// <summary>QT 设置页「基础」页签的开关清单。元组为（键, 显示名, 技能ID），技能ID 为 0 时不校验解锁。</summary>
    public static (string key, string label, uint skill)[] QtTab基础 { get; private set; } = Array.Empty<(string, string, uint)>();

    /// <summary>QT 设置页「技能」页签的开关清单，格式同 QtTab基础。</summary>
    public static (string key, string label, uint skill)[] QtTab技能 { get; private set; } = Array.Empty<(string, string, uint)>();

    /// <summary>QT 设置页「资源」页签的开关清单，格式同 QtTab基础。</summary>
    public static (string key, string label, uint skill)[] QtTab资源 { get; private set; } = Array.Empty<(string, string, uint)>();

    /// <summary>注入本职业的全部环境，由 Nag0miUIFramework.Configure 转发调用。</summary>
    internal static void Configure(
        string jobTag,
        string jobName,
        IReadOnlyDictionary<string, bool> qtAll,
        Func<string, bool> qtIsMetaKey,
        Func<string, int, bool> qtIsVisibleInMode,
        Func<string, bool> qtDefault,
        IReadOnlyDictionary<string, (string key, bool invert)[]> qtCascadeRules,
        string[] hotkeyNames,
        System.Action<Nag0miUIHotkeyBuilder>? buildHotkeys,
        (string key, string label, uint skill)[]? qtTab基础 = null,
        (string key, string label, uint skill)[]? qtTab技能 = null,
        (string key, string label, uint skill)[]? qtTab资源 = null,
        int modeCount = 2,
        string[]? modeNames = null,
        string? author = null,
        Func<string, (uint iconId, bool isGameIcon, string? marker)>? qtIconResolver = null,
        Func<string, int, bool>? qtDefaultVisible = null,
        (string label, System.Action draw)[]? extraTabs = null,
        System.Action? cycleSettingsTab = null,
        System.Action? cycleMode = null,
        Func<string>? currentModeLabel = null,
        (uint Id, string Name, PromeRotation.Data.ActionType Type)[]? customHotkeySkills = null,
        IReadOnlyDictionary<uint, (uint BuffId, bool SelfOnly)>? hotkeyActiveBuffs = null)
    {
        JobTag = jobTag;
        JobName = jobName;
        if (author != null) 作者 = author;
        ModeCount = Math.Max(1, modeCount);
        ModeNames = modeNames ?? Array.Empty<string>();
        QtAll = qtAll;
        QtIsMetaKey = qtIsMetaKey;
        QtIsVisibleInMode = qtIsVisibleInMode;
        QtDefault = qtDefault;
        QtCascadeRules = qtCascadeRules;
        HotkeyNames = hotkeyNames;
        BuildHotkeys = buildHotkeys;
        QtIconResolver = qtIconResolver;
        QtDefaultVisible = qtDefaultVisible ?? ((_, _) => true);
        if (qtTab基础 != null) QtTab基础 = qtTab基础;
        if (qtTab技能 != null) QtTab技能 = qtTab技能;
        if (qtTab资源 != null) QtTab资源 = qtTab资源;
        ExtraTabs = extraTabs ?? Array.Empty<(string, System.Action)>();
        CycleSettingsDraw = cycleSettingsTab;
        CycleMode = cycleMode;
        CurrentModeLabel = currentModeLabel;
        CustomHotkeySkills = customHotkeySkills ?? Array.Empty<(uint, string, PromeRotation.Data.ActionType)>();
        HotkeyActiveBuffs = hotkeyActiveBuffs ?? new Dictionary<uint, (uint, bool)>();
    }
}
