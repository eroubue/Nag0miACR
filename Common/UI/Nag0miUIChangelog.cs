using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Nag0mi.Common.Data;

namespace Nag0mi.Common.UI;

// 设置窗「更新日志」页签：按版本倒序列出日期 / 版本号 / 内容，
// 新增行绿色「+」，移除/修复行红色「-」，说明行默认色。
public static class Nag0miUIChangelog
{
    private static readonly Vector4 AddColor = new(0.35f, 1f, 0.35f, 1f);
    private static readonly Vector4 RemoveColor = new(1f, 0.4f, 0.4f, 1f);
    private static readonly Vector4 HeaderColor = new(1f, 0.85f, 0.4f, 1f);

    // 日志行预拼缓存：条目编译期固定, 首次绘制时拼好全部行文本, 之后每帧直接绘制
    private enum 行种类 { 版本头, 新增, 移除, 说明, 间隔 }
    private static readonly string 当前版本行 = $"当前版本 v{AcrChangelog.Version}";
    private static List<(行种类 种类, string 文本)>? 行缓存;

    private static List<(行种类, string)> 取行缓存()
    {
        if (行缓存 != null) return 行缓存;
        var rows = new List<(行种类, string)>();
        foreach (var entry in AcrChangelog.Entries)
        {
            rows.Add((行种类.版本头, $"v{entry.Version} — {entry.Date}"));
            foreach (var (kind, text) in entry.Lines)
            {
                rows.Add(kind switch
                {
                    AcrChangelog.LineKind.Add => (行种类.新增, $"  + {text}"),
                    AcrChangelog.LineKind.Remove => (行种类.移除, $"  - {text}"),
                    _ => (行种类.说明, $"    {text}"),
                });
            }
            rows.Add((行种类.间隔, ""));
        }
        return 行缓存 = rows;
    }

    public static void Draw()
    {
        ImGui.Dummy(new Vector2(0, 5));
        ImGui.TextColored(HeaderColor, 当前版本行);
        ImGui.Separator();

        foreach (var (种类, 文本) in 取行缓存())
        {
            switch (种类)
            {
                case 行种类.版本头:
                    ImGui.TextColored(HeaderColor, 文本);
                    break;
                case 行种类.新增:
                    ImGui.TextColored(AddColor, 文本);
                    break;
                case 行种类.移除:
                    ImGui.TextColored(RemoveColor, 文本);
                    break;
                case 行种类.说明:
                    ImGui.TextUnformatted(文本);
                    break;
                default:
                    ImGui.Spacing();
                    break;
            }
        }

        ImGui.Dummy(new Vector2(0, 5));
    }
}
