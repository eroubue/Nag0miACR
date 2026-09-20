// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Nag0mi.Common.UI;

// 侧边栏布局：左侧竖排导航 + 竖分隔线 + 右侧滚动内容区。
// 全部颜色取自 SimplePalette（固定暗色方案），本文件不含硬编码颜色。
public abstract partial class SettingsWindowBase
{
    // 侧边栏 chrome 增量样式的颜色 Push 数量（Pop 时与 BaseStyleColorCount 相加）。
    private const int SidebarChromeColorCount = 7;

    // 侧边栏按钮出现/消失动效状态（按标签记忆: T 0=收起 1=完全展开, 时间制缓动趋近目标值）。
    // 新出现的标签从 0 展开（高度+透明度渐入）; 被移除的标签淡出到 0 后才从序列删除。
    private sealed class TabAnimState
    {
        public float T = 1f;
        public float 目标 = 1f;
        public float 起始T;
        public double 起始时刻;
        public int LastIndex;
    }

    private readonly Dictionary<string, TabAnimState> tabAnims = new();
    private HashSet<string>? prevTabLabels;

    // 出现/收起动效时长（秒）
    private const float TabAnimDuration = 0.22f;

    // 时间制 smoothstep 缓动（起步/收尾轻, 中段匀速）。
    // 指数阻尼在这里上下卡顿: 首帧位移最大（点开瞬间跳一下）, 尾部长时间亚像素爬行
    // 又被 ImGui 的整数取整吞掉, 视觉上变成「跳-停-跳」; 固定时长缓动每帧位移均匀可控。
    private static void 推进TabAnim(TabAnimState st, float 目标, double now)
    {
        if (st.目标 != 目标)
        {
            st.目标 = 目标;
            st.起始T = st.T;
            st.起始时刻 = now;
        }
        if (st.T == st.目标) return;
        var t = Math.Clamp((float)((now - st.起始时刻) / TabAnimDuration), 0f, 1f);
        st.T = st.起始T + (st.目标 - st.起始T) * (t * t * (3f - 2f * t));
    }

    // 侧边栏布局：左侧竖排导航 + 右侧内容区。
    private void DrawSidebarLayout()
    {
        const float navWidth = 120f;
        const float navItemHeight = 28f;

        ImGui.BeginChild("##side_nav", new Vector2(navWidth, 0f), false,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        try
        {
            // 导航文字左对齐
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0f, 0.5f));

            var now = ImGui.GetTime();
            var labels = AllTabs;

            // 首帧全部直接落位，不做入场动画
            prevTabLabels ??= new HashSet<string>(labels);

            // 出现走展开动效; 收起直接消失（不保留淡出项, 不推挤下方按钮）
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (!tabAnims.TryGetValue(label, out var st))
                    tabAnims[label] = st = new TabAnimState
                    {
                        T = prevTabLabels.Contains(label) ? 1f : 0f,
                        起始T = prevTabLabels.Contains(label) ? 1f : 0f,
                        起始时刻 = now,
                    };
                st.LastIndex = i;
                推进TabAnim(st, 1f, now);
            }
            var dead = tabAnims.Where(kv => Array.IndexOf(labels, kv.Key) < 0).Select(kv => kv.Key).ToList();
            foreach (var key in dead) tabAnims.Remove(key);

            prevTabLabels = new HashSet<string>(labels);

            var drawn = 0;
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                var t = tabAnims[label].T;
                if (t <= 0f) continue;

                if (drawn > 0)
                {
                    if (t >= 1f) ImGui.Spacing();
                    else ImGui.Dummy(new Vector2(1f, ImGui.GetStyle().ItemSpacing.Y * t));
                }
                drawn++;

                var isActive = i == currentTab;
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

                // 展开中: 高度插值 + 整体透明渐入
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, t);
                if (ImGui.Button(label, new Vector2(-1f, navItemHeight * t)))
                    currentTab = i;
                ImGui.PopStyleVar();

                ImGui.PopStyleColor(4);
            }
            ImGui.PopStyleVar();
        }
        finally
        {
            ImGui.EndChild();
        }

        // 竖分隔线
        ImGui.SameLine(0f, 0f);
        var linePos = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddLine(
            linePos + new Vector2(5f, 0f),
            linePos + new Vector2(5f, ImGui.GetContentRegionAvail().Y),
            SimplePalette.ToU32(SimplePalette.Border));

        // 右侧间距与左侧对齐：左 = WindowPadding.X(16)，右 = 按钮到内容区同样 16
        ImGui.SameLine(0f, 16f);
        DrawContentChild();
    }
}
