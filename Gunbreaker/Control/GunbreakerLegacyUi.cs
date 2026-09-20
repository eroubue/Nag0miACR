using System.Text.Json.Nodes;
using Nag0mi.Gunbreaker.Data;

namespace Nag0mi.Gunbreaker.Control;

// v2 及更早 Nag0mi.Gunbreaker.json 中的 UI 部分（QT 面板布局/顺序、热键面板布局、
// 悬浮条位置、各模式 QT 默认值）。这些状态现由框架 ACRConfig\Nag0mi\GNB.json 持有；
// 加载旧版文件时由 GunbreakerConfigStore 一次性抽出，调用方导入框架设置后存档升为 v3。
internal sealed class GunbreakerLegacyUi
{
    public List<string> QtOrder { get; } = new();
    public int? QtPanelColumns { get; private set; }
    public float? QtPanelSpacing { get; private set; }
    public float? QtPanelScale { get; private set; }
    public bool QtPanelLockOrder { get; private set; }
    public int? HotkeyColumns { get; private set; }
    public float? HotkeyButtonSize { get; private set; }
    public float? HotkeySpacing { get; private set; }

    // 悬浮条归一化吸附位置；Side 直接沿用 SnapSide 数值（0=不吸附 1=左 2=右）
    public int? WindowSide { get; private set; }
    public float WindowRelativeX { get; private set; } = 1f;
    public float WindowRelativeY { get; private set; } = 0.5f;

    // 各模式 QT 默认值快照：模式索引（Normal=0/HighEnd=1/Custom=2）→ (QT键 → 默认值)
    public Dictionary<int, Dictionary<string, bool>> QtDefaultsByMode { get; } = new();

    // 从旧版配置文本中抽出 UI 状态；无任何可识别字段（或解析失败）返回 null。
    public static GunbreakerLegacyUi? TryParse(string json)
    {
        try
        {
            if (JsonNode.Parse(json) is not JsonObject root) return null;
            var ui = new GunbreakerLegacyUi();
            var found = false;

            if (root["QtPanel"] is JsonObject qtPanel)
            {
                found = true;
                ui.QtPanelColumns = GetInt(qtPanel, "Columns");
                ui.QtPanelSpacing = GetFloat(qtPanel, "Spacing");
                ui.QtPanelScale = GetFloat(qtPanel, "Scale");
                ui.QtPanelLockOrder = qtPanel["LockOrder"] is JsonValue lo
                    && lo.TryGetValue<bool>(out var lockOrder) && lockOrder;
                if (qtPanel["Order"] is JsonArray order)
                    foreach (var node in order)
                        if (node is JsonValue v && v.TryGetValue<string>(out var key)
                            && GunbreakerQtDefaults.All.ContainsKey(key) && !ui.QtOrder.Contains(key))
                            ui.QtOrder.Add(key);
            }

            if (root["HotkeyPanel"] is JsonObject hotkeyPanel)
            {
                found = true;
                ui.HotkeyColumns = GetInt(hotkeyPanel, "Columns");
                ui.HotkeyButtonSize = GetFloat(hotkeyPanel, "ButtonSize");
                ui.HotkeySpacing = GetFloat(hotkeyPanel, "Spacing");
            }

            if (root["Window"] is JsonObject window)
            {
                found = true;
                // 旧配置经 JsonStringEnumConverter 存枚举名
                ui.WindowSide = window["Side"] is JsonValue s && s.TryGetValue<string>(out var side)
                    ? side switch { "Left" => 1, "Right" => 2, _ => 0 }
                    : null;
                if (GetFloat(window, "RelativeX") is { } rx) ui.WindowRelativeX = rx;
                if (GetFloat(window, "RelativeY") is { } ry) ui.WindowRelativeY = ry;
            }

            var modes = new[] { "Normal", "HighEnd", "Custom" };
            for (var i = 0; i < modes.Length; i++)
            {
                if (root[modes[i]]?["QuickToggles"] is not JsonObject toggles) continue;
                found = true;
                var dict = new Dictionary<string, bool>();
                foreach (var (key, node) in toggles)
                    if (GunbreakerQtDefaults.All.ContainsKey(key)
                        && node is JsonValue v && v.TryGetValue<bool>(out var value))
                        dict[key] = value;
                if (dict.Count > 0) ui.QtDefaultsByMode[i] = dict;
            }

            return found ? ui : null;
        }
        catch
        {
            return null;
        }
    }

    private static int? GetInt(JsonObject obj, string name) =>
        obj[name] is JsonValue v && v.TryGetValue<int>(out var value) ? value : null;

    private static float? GetFloat(JsonObject obj, string name) =>
        obj[name] is JsonValue v && v.TryGetValue<float>(out var value) ? value : null;
}
