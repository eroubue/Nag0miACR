// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Text.Json;
using ECommons.Logging;
using PromeRotation.Config;
using PromeRotation.Data;

namespace Nag0mi.Common.Data;

// 职业运行期设置（单例，JSON 持久化，按职业分文件: {JobTag}.json）。
// 面板布局 / QT 显隐与默认值 / 排序等框架配置在此。
// 模式泛化为模式索引 int（0..ModeCount-1，如 GNB 的 Normal/HighEnd/Custom = 0/1/2）。
public class Nag0miUISettings
{
    private static Nag0miUISettings? instance;
    private static string? loadedJob;

    // 按当前职业取设置实例: 职业环境(JobTag)变化时自动重载对应文件（多职业共存,
    // 各职业的布局/QT 快照互不污染）。
    public static Nag0miUISettings Instance
    {
        get
        {
            var job = 职业文件名;
            if (instance == null || loadedJob != job)
            {
                instance = Load();
                loadedJob = job;
            }
            return instance;
        }
    }

    // ============================================================
    // === 职业框架设置 ===
    // ============================================================

    // 当前模式索引（0..ModeCount-1，初次访问默认 0，面板顶部按钮可切换）
    public int ModeIndex = 0;

    // 战斗控制悬浮条位置（窗口拖动后落盘）
    public System.Numerics.Vector2? 控制条位置;

    // 战斗控制悬浮条归一化吸附位置（竖条左右吸附用; 吸附侧 0=不吸附 1=左 2=右, 对应 Gunbreaker SnapSide;
    // 拆开存基础类型保持 JSON 可序列化且不外泄 internal 类型）
    public int 控制条吸附侧 = 2;
    public float 控制条相对X = 1f;
    public float 控制条相对Y = 0.5f;

    // ============================================================
    // === 热键面板（每职业一套布局） ===
    // ============================================================
    // 热键面板每行按钮数
    public int HotkeyColumns = 5;

    // 热键按钮间距(px)
    public int HotkeySpacing = 5;

    // 热键缩放(%)（基准按钮 45px）
    public int HotkeyScalePercent = 100;

    // 隐藏的热键按钮名（只控制显隐，不分模式）
    public HashSet<string> HiddenHotkeys = new();

    // 热键面板按钮自定义顺序（完整名字序; 未调整过为空 = 按热键表定义顺序显示）
    public List<string> HotkeyOrder = new();

    // 锁定热键排序: 开启后热键悬浮面板按钮不可右键拖拽换位（防战斗误拖）
    public bool HotkeyPanelOrderLocked = false;

    // 热键悬浮面板位置（左键拖动缝隙落盘; null = 首次居中）
    public System.Numerics.Vector2? 热键面板位置;

    // 自定义热键（设置页 Hotkey 页管理：技能+目标 组合; 显示名唯一, 参与排序与显隐）
    public List<CustomHotkeyEntry> CustomHotkeys = new();

    // ============================================================
    // === QT 面板（每职业一套布局; QT面板页滑块可调） ===
    // ============================================================
    // QT 面板每行按钮数
    public int QtPanelColumns = 3;

    // QT 面板按钮间距(px)
    public int QtPanelSpacing = 10;

    // QT 面板缩放(%)（基准按钮 112×34px）
    public int QtPanelScalePercent = 100;

    // ============================================================
    // === QT 面板排列顺序（每职业一套; QT面板页上移/下移 + 悬浮面板拖拽可调） ===
    // ============================================================
    // QT 面板按钮自定义顺序（完整键序; 未调整过为空 = 按 QT 表定义顺序显示）
    public List<string> QtOrder = new();

    // 锁定 QT 排序: 开启后悬浮面板按钮不可拖拽换位（防战斗误拖）, 设置页上移/下移不受影响
    public bool QtPanelOrderLocked = false;

    // QT 悬浮面板位置（左键拖动缝隙落盘; null = 首次居中）
    public System.Numerics.Vector2? Qt面板位置;

    // ============================================================
    // === QT 面板按钮显隐（按模式索引各存一套；键=QT名; 缺省=显示。只控制显隐, 不影响开关状态） ===
    // ============================================================
    // 按模式索引的 QT 显隐记录: 模式索引 → (QT键 → 是否显示)
    public Dictionary<int, Dictionary<string, bool>> QtVisibleByMode = new();

    // 当前模式的显隐记录字典（不存在时惰性创建）
    private Dictionary<string, bool> CurrentQtVisibleDict => QtVisibleForMode(ModeIndex);

    // 取指定模式索引的显隐记录字典（不存在时创建并登记）
    private Dictionary<string, bool> QtVisibleForMode(int modeIndex)
    {
        if (!QtVisibleByMode.TryGetValue(modeIndex, out var dict))
            QtVisibleByMode[modeIndex] = dict = new Dictionary<string, bool>();
        return dict;
    }

    // 该 QT 按钮是否显示在 QT 面板（按当前模式取对应记录；未配置过 = 回落职业注入的
    // 出厂默认显隐 QtDefaultVisible，缺省全显示）
    public bool IsQtVisible(string key)
        => CurrentQtVisibleDict.TryGetValue(key, out var v) ? v : Nag0miUIJobEnv.QtDefaultVisible(key, ModeIndex);

    // 设置【当前模式】的 QT 按钮显隐并持久化（同步到 PR 本体面板的 HiddenQts）
    public void SetQtVisible(string key, bool visible)
    {
        CurrentQtVisibleDict[key] = visible;
        Save();
        SyncQtHiddenToPr();
    }

    // 把【当前模式】的显隐配置同步到 PR 本体面板的 HiddenQts 集合（数据源 = 当前职业 QT 表）。
    // 切换模式后由 重建QT可见性 重新调用，即实现「切模式自动套用该模式的显隐记录」。
    public void SyncQtHiddenToPr()
    {
        var hidden = PromeSettings.Instance.HiddenQts;
        foreach (var key in Nag0miUIJobEnv.QtAll.Keys)
        {
            if (Nag0miUIJobEnv.QtIsMetaKey(key)) continue;
            var wantHide = !IsQtVisible(key);
            var has = hidden.Contains(key);
            if (wantHide && !has) hidden.Add(key);
            else if (!wantHide && has) hidden.Remove(key);
        }
    }

    // ============================================================
    // === QT 排列顺序（悬浮面板 / 各设置页统一取此顺序） ===
    // ============================================================

    // 当前职业 QT 键的显示顺序：QtOrder 中仍有效的键按其顺序在前,
    // QT 表新增的键按定义顺序补尾; QtOrder 为空时即 QT 表定义顺序（与旧版行为一致）。
    public List<string> GetOrderedQtKeys()
        => OrderMergeHelper.MergeOrder(QtOrder, Nag0miUIJobEnv.QtAll.Keys);

    // 把指定 QT 移动到面板上的目标位置（插入语义）。
    // 目标索引按面板当前显示的格子计数，被隐藏的键不占格子；隐藏键留在原槽位，
    // 不随可见键的移动而重排。移动后落盘并重建 QT 注册顺序。
    public void MoveQt(string key, int 可见目标索引)
    {
        var qt = PromeSettings.Instance.QuickToggles;
        var hidden = PromeSettings.Instance.HiddenQts;
        var full = GetOrderedQtKeys();
        // 面板可见序列 = 已注册且未被显隐隐藏（模式不可见的键本就不会被重建注册）
        var visibleSet = new HashSet<string>(full.Where(k => qt.ContainsKey(k) && !hidden.Contains(k)));
        var result = OrderMergeHelper.MoveWithinVisible(full, visibleSet, key, 可见目标索引);
        if (result == null) return;
        QtOrder = result;
        Save();
        APIHelper.重建QT可见性();
    }

    // ============================================================
    // === 热键排列顺序（悬浮面板 / 热键设置页统一取此顺序） ===
    // ============================================================

    // 当前职业热键名的显示顺序：HotkeyOrder 中仍有效的名字按其顺序在前,
    // 热键表新增的名字按定义顺序补尾; HotkeyOrder 为空时即热键表定义顺序（与旧版行为一致）。
    // 全集 = 职业内置热键 + 自定义热键（按添加顺序补尾）。
    public List<string> GetOrderedHotkeyNames()
        => OrderMergeHelper.MergeOrder(HotkeyOrder,
            Nag0miUIJobEnv.HotkeyNames.Concat(CustomHotkeys.Select(c => c.Name)));

    // 把指定热键移动到面板上的目标位置（插入语义），索引口径与 MoveQt 相同：
    // 按面板当前显示的格子计数，被隐藏的名字不占格子。面板每帧按此顺序重排，落盘即可。
    public void MoveHotkey(string name, int 可见目标索引)
    {
        var full = GetOrderedHotkeyNames();
        // 面板可见序列 = 未被显隐隐藏
        var visibleSet = new HashSet<string>(full.Where(k => !HiddenHotkeys.Contains(k)));
        var result = OrderMergeHelper.MoveWithinVisible(full, visibleSet, name, 可见目标索引);
        if (result == null) return;
        HotkeyOrder = result;
        Save();
    }

    // ============================================================
    // === QT 默认值持久化（按模式索引） ===
    // ============================================================

    // 按模式索引的 QT 默认值快照: 模式索引 → (QT键 → 默认值)
    public Dictionary<int, Dictionary<string, bool>> QtDefaultsByMode = new();

    // ============================================================
    // === 方法 ===
    // ============================================================

    // 取指定模式索引的默认值快照字典（不存在时创建并登记）
    private Dictionary<string, bool> QtDefaultsForMode(int modeIndex)
    {
        if (!QtDefaultsByMode.TryGetValue(modeIndex, out var dict))
            QtDefaultsByMode[modeIndex] = dict = new Dictionary<string, bool>();
        return dict;
    }

    // 当前模式的 QT 默认值快照
    public Dictionary<string, bool> GetCurrentModeDefaults()
        => QtDefaultsForMode(ModeIndex);

    // 保存当前 QT 状态到指定模式的默认值快照。
    // 注意: 只采集目标模式下可见的 QT——不可见的 QT 读不到实时值，跳过以免把已有默认值覆盖成 false。
    public void SaveQtSnapshot(int modeIndex)
    {
        var dict = QtDefaultsForMode(modeIndex);
        foreach (var key in Nag0miUIJobEnv.QtAll.Keys)
        {
            if (Nag0miUIJobEnv.QtIsMetaKey(key)) continue;
            if (!Nag0miUIJobEnv.QtIsVisibleInMode(key, modeIndex)) continue;
            dict[key] = PromeSettings.Instance.GetQt(key);
        }
    }

    // 从指定模式快照恢复 QT 状态
    public void RestoreQtSnapshot(int modeIndex)
    {
        var dict = QtDefaultsForMode(modeIndex);
        if (dict.Count == 0)
        {
            foreach (var key in Nag0miUIJobEnv.QtAll.Keys)
            {
                if (Nag0miUIJobEnv.QtIsMetaKey(key)) continue;
                APIHelper.设置QT(key, Nag0miUIJobEnv.QtDefault(key));
            }
        }
        else
        {
            foreach (var key in Nag0miUIJobEnv.QtAll.Keys)
            {
                if (Nag0miUIJobEnv.QtIsMetaKey(key)) continue;
                APIHelper.设置QT(key, dict.TryGetValue(key, out var v) ? v : Nag0miUIJobEnv.QtDefault(key));
            }
        }
    }

    // 模式切换（QT 自动重置为目标模式的默认值）
    public void SwitchMode(int modeIndex)
    {
        if (modeIndex < 0 || modeIndex >= Nag0miUIJobEnv.ModeCount) return;
        if (ModeIndex == modeIndex) return;
        ModeIndex = modeIndex;
        // 切模式即重置为目标模式的默认 QT（QT面板页默认值子视图配置，缺省走出厂默认）；
        // 不再用切换瞬间的实时状态覆盖默认值——想存当前状态请用 QT面板页的「从当前QT导入」
        RestoreQtSnapshot(modeIndex);
        // 重建 QT 可见性（按模式归属过滤）——否则面板不同步
        APIHelper.重建QT可见性();
        Save();
    }


    // ============================================================
    // === JSON 持久化 ===
    // ============================================================
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
    };

    // 配置目录（稳定路径，与进程无关）：
    // 插件配置目录\(宿主)\Settings\ACRConfig\Nag0mi——宿主 ACRAuthorSetting 按作者约定的
    // 配置目录; 作者身份经 Nag0miUIJobEnv.Configure 注入（固定为「Nag0mi」,
    // 与 RotationMetadata 的 Author 一致）; 目录不存在时由 FilePath 首次访问自动建。
    public static string SettingsDirectory
    {
        get
        {
            return ACRAuthorSetting.GetSettingsDirectory(Nag0miUIJobEnv.作者);
        }
    }

    // 配置文件名按当前职业: JobTag 由使用方经 Nag0miUIJobEnv.Configure 注入, 以它为准。
    // 不读 Core.Me——宿主切职业/登录加载存在 ClassJob.RowId 未落地(仍报旧职业)的窗口期,
    // 登录或切职业的瞬间读取玩家职业可能拿到旧值，据此加载会把配置绑到错误职业的文件上。
    public static string 职业文件名 => Nag0miUIJobEnv.JobTag;

    public static string FilePath
    {
        get
        {
            try
            {
                var dir = SettingsDirectory;
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);   // 已存在时为 no-op
                return System.IO.Path.Combine(dir, 职业文件名 + ".json");
            }
            catch
            {
                return System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                    "XIVLauncherCN", "pluginConfigs", "PromeRotation", "Settings", "ACRConfig",
                    Nag0miUIJobEnv.作者, $"{Nag0miUIJobEnv.作者}.Settings.json");
            }
        }
    }

    public static Nag0miUISettings Load()
    {
        try
        {
            if (System.IO.File.Exists(FilePath))
            {
                var json = System.IO.File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<Nag0miUISettings>(json, JsonOptions);
                if (s != null)
                {
                    return Normalize(s);
                }
            }
            else
            {
                // 首次使用（用户配置不存在）：加载内嵌默认配置并立即落盘生成用户文件
                var s = LoadEmbeddedDefaults();
                if (s != null)
                {
                    var loaded = Normalize(s);
                    loaded.Save();
                    PluginLog.Log($"[{Nag0miUIJobEnv.作者}] 首次使用，已写入默认配置: {FilePath}");
                    return loaded;
                }
            }
        }
        catch (Exception e) { PluginLog.Error($"[{Nag0miUIJobEnv.作者}] 设置加载失败: {e.Message}"); }
        return new Nag0miUISettings();
    }

    // 读取内嵌的首次使用默认配置（Resources/Default{职业文件名}.json，随 DLL 发布，按职业各一份）
    private static Nag0miUISettings? LoadEmbeddedDefaults()
    {
        try
        {
            var json = ReadEmbeddedDefaultJson($"Default{职业文件名}.json");
            return json == null ? null : JsonSerializer.Deserialize<Nag0miUISettings>(json, JsonOptions);
        }
        catch (Exception e)
        {
            PluginLog.Error($"[{Nag0miUIJobEnv.作者}] 内嵌默认配置解析失败: {e.Message}");
            return null;
        }
    }

    // 读取内嵌出厂配置资源文本（Resources/Default*.json，随 DLL 发布; 资源缺失返回 null）。
    // Nag0miUISettings（按职业）与 Nag0miUICommonSettings（通用 Common.json）的首次使用出厂配置共用此入口。
    internal static string? ReadEmbeddedDefaultJson(string fileName)
    {
        try
        {
            using var stream = typeof(Nag0miUISettings).Assembly
                .GetManifestResourceStream($"Nag0mi.Resources.{fileName}");
            if (stream == null)
            {
                PluginLog.Warning($"[{Nag0miUIJobEnv.作者}] 未找到内嵌默认配置资源: {fileName}");
                return null;
            }
            using var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception e)
        {
            PluginLog.Error($"[{Nag0miUIJobEnv.作者}] 内嵌默认配置读取失败: {fileName}, {e.Message}");
            return null;
        }
    }

    // 载入后的统一规整：按当前职业 QT 表修剪显隐记录与排序表
    private static Nag0miUISettings Normalize(Nag0miUISettings s)
    {
        var qt = Nag0miUIJobEnv.QtAll;
        // ModeIndex 越界（手改 JSON 或 ModeCount 调整后的旧配置）一律回落 0
        if (s.ModeIndex < 0 || s.ModeIndex >= Nag0miUIJobEnv.ModeCount)
            s.ModeIndex = 0;
        // QtVisibleByMode 显隐记录修剪: 剔除越界模式索引与当前职业 QT 表已不存在的死键
        // （出厂/旧版配置残留; 缺省未配置 = 显示, 无需补齐）
        foreach (var m in s.QtVisibleByMode.Keys.Where(m => m < 0 || m >= Nag0miUIJobEnv.ModeCount).ToList())
            s.QtVisibleByMode.Remove(m);
        foreach (var dict in s.QtVisibleByMode.Values)
            foreach (var k in dict.Keys.Where(k => !qt.ContainsKey(k)).ToList())
                dict.Remove(k);
        // QtDefaultsByMode 同理修剪
        foreach (var m in s.QtDefaultsByMode.Keys.Where(m => m < 0 || m >= Nag0miUIJobEnv.ModeCount).ToList())
            s.QtDefaultsByMode.Remove(m);
        foreach (var dict in s.QtDefaultsByMode.Values)
            foreach (var k in dict.Keys.Where(k => !qt.ContainsKey(k)).ToList())
                dict.Remove(k);
        // QtOrder 修剪: 当前职业 QT 表已不存在的键剔除、重复项去重
        s.QtOrder = s.QtOrder.Where(qt.ContainsKey).Distinct().ToList();
        // 死亡队友目标已下线: 剔除使用它的自定义热键(旧配置残留), 并清理其显隐/排序记录
        var deadNames = s.CustomHotkeys.Where(c => c.Target == CustomHotkeyTarget.DeadParty)
            .Select(c => c.Name).ToList();
        if (deadNames.Count > 0)
        {
            s.CustomHotkeys.RemoveAll(c => c.Target == CustomHotkeyTarget.DeadParty);
            foreach (var n in deadNames)
            {
                s.HiddenHotkeys.Remove(n);
                s.HotkeyOrder.Remove(n);
            }
        }
        // CustomHotkeys 去重：空名/同名条目只留第一条（名字是排序/显隐/ImGui ID 的键, 防手改 JSON 出重名）
        var customSeen = new HashSet<string>(StringComparer.Ordinal);
        s.CustomHotkeys = s.CustomHotkeys
            .Where(c => !string.IsNullOrEmpty(c.Name) && customSeen.Add(c.Name)).ToList();
        // HotkeyOrder 修剪: 当前职业热键表与自定义热键已不存在的名字剔除、重复项去重
        var hotkeyValid = new HashSet<string>(Nag0miUIJobEnv.HotkeyNames, StringComparer.Ordinal);
        foreach (var c in s.CustomHotkeys) hotkeyValid.Add(c.Name);
        s.HotkeyOrder = s.HotkeyOrder.Where(hotkeyValid.Contains).Distinct().ToList();
        return s;
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, JsonOptions);
            var dir = System.IO.Path.GetDirectoryName(FilePath);
            if (dir != null && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(FilePath, json);
        }
        catch { /* 写失败静默 */ }
    }
}
