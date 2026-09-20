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

    public static void Draw()
    {
        ImGui.Dummy(new Vector2(0, 5));
        ImGui.TextColored(HeaderColor, $"当前版本 v{AcrChangelog.Version}");
        ImGui.Separator();

        foreach (var entry in AcrChangelog.Entries)
        {
            ImGui.TextColored(HeaderColor, $"v{entry.Version} — {entry.Date}");
            foreach (var (kind, text) in entry.Lines)
            {
                switch (kind)
                {
                    case AcrChangelog.LineKind.Add:
                        ImGui.TextColored(AddColor, $"  + {text}");
                        break;
                    case AcrChangelog.LineKind.Remove:
                        ImGui.TextColored(RemoveColor, $"  - {text}");
                        break;
                    default:
                        ImGui.TextUnformatted($"    {text}");
                        break;
                }
            }
            ImGui.Spacing();
        }

        ImGui.Dummy(new Vector2(0, 5));
    }
}
