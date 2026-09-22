// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Nag0mi.Common.UI;

// 侧边栏布局：左侧竖排导航 + 竖分隔线 + 右侧滚动内容区。
// 全部颜色取自 SimplePalette（固定暗色方案），本文件不含硬编码颜色。
public abstract partial class SettingsWindowBase
{
    // 侧边栏 chrome 增量样式的颜色 Push 数量（Pop 时与 BaseStyleColorCount 相加）。
    private const int SidebarChromeColorCount = 4;

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
        // 外层窗口原点与拖动位移：拖动把手在子窗内, 位移须在 EndChild 后施加到外层窗口
        var outerPos = ImGui.GetWindowPos();
        var dragDelta = Vector2.Zero;

        // 侧边栏页签用标题书法字体（未就绪时自动回落默认字体）
        var titleFont = ShuimoFont.Title;
        var fontPop = titleFont is { Available: true } ? titleFont.Push() : null;
        try
        {
            var labels = AllTabs;

            // 页签行高跟随实际字号：22px 书法字体塞 28px 行高过挤，按字号+内边距+余量取高，
            // 默认字体回落 28px；侧栏宽度跟随最宽文字——固定 120px 在书法字体/宿主全局缩放下
            // 会裁掉最宽页签的末字（如「面板控制」的「制」），下限 120px
            var navItemHeight = MathF.Max(28f,
                ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2f + 4f);
            var navWidth = 120f;
            for (var i = 0; i < labels.Length; i++)
            {
                var indent = TabGroup(i) != null ? 12f : 0f;   // 分组子项右缩进
                var w = ImGui.CalcTextSize(labels[i]).X + 8f + indent + 8f;
                if (w > navWidth) navWidth = MathF.Ceiling(w);
                if (TabGroup(i) is { } g && (i == 0 || TabGroup(i - 1) != g))
                {
                    var gw = ImGui.CalcTextSize(g).X + 16f;
                    if (gw > navWidth) navWidth = MathF.Ceiling(gw);
                }
            }

            ImGui.BeginChild("##side_nav", new Vector2(navWidth, 0f), false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            try
            {
                var now = ImGui.GetTime();

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
                string? prevGroup = null;
                for (var i = 0; i < labels.Length; i++)
                {
                    var label = labels[i];
                    var t = tabAnims[label].T;
                    if (t <= 0f) continue;

                    // 分组标题（可点击：跳转组内首个子栏; 透明底 + 次级文字色, 悬停淡染）
                    var group = TabGroup(i);
                    if (group != null && group != prevGroup)
                    {
                        if (drawn > 0) ImGui.Spacing();
                        if (DrawNavRow($"##group{i}", group, 24f, 1f, SimplePalette.TextSecondary, active: false))
                            currentTab = i;
                        drawn++;
                    }
                    prevGroup = group;

                    if (drawn > 0)
                    {
                        if (t >= 1f) ImGui.Spacing();
                        else ImGui.Dummy(new Vector2(1f, ImGui.GetStyle().ItemSpacing.Y * t));
                    }
                    drawn++;

                    var isActive = i == currentTab;
                    if (DrawNavRow($"##tab{i}", label, navItemHeight * t, t,
                            isActive ? SimplePalette.NavActiveText : SimplePalette.TextSecondary,
                            isActive, group != null ? 12f : 0f))
                        currentTab = i;
                }

                // 页签下方的剩余空白区作为窗口拖动把手（无标题栏后唯一的移动途径）,
                // 填满侧边栏剩余高度; 空间不足一行的极端情况跳过
                var rest = ImGui.GetContentRegionAvail();
                if (rest.Y > 4f)
                {
                    ImGui.InvisibleButton("##nav_drag", new Vector2(-1f, -1f));
                    if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                        dragDelta = ImGui.GetIO().MouseDelta;
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("按住拖动窗口");
                }
            }
            finally
            {
                ImGui.EndChild();
            }
        }
        finally
        {
            fontPop?.Dispose();
        }

        // 空白区拖动：位移施加到外层窗口（子窗内 SetWindowPos 只会动子窗）
        if (dragDelta != Vector2.Zero)
            ImGui.SetWindowPos(outerPos + dragDelta);

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

    // 侧栏导航行：隐形按钮承载悬停/点击，文字用 AddText 手画——ImGui.Button 会把文字
    // 裁剪到按钮矩形内，书法字体（22px，随宿主全局缩放放大）超出固定行高时字形顶部被裁掉；
    // 手动绘制只受子窗口裁剪，配合动态侧栏宽度不再裁字。
    // 返回 true = 本行被点击。active 项悬停淡染改用强调色并画左侧 3px 血红竖条;
    // alpha 跟随页签展开动效（背景/文字/竖条同步渐显）。
    private static bool DrawNavRow(string id, string text, float height, float alpha, Vector4 textColor,
        bool active, float indent = 0f)
    {
        if (indent > 0f)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + indent);
        ImGui.InvisibleButton(id, new Vector2(-1f, height));
        var clicked = ImGui.IsItemClicked();
        var hovered = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();

        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();

        // 悬停/按下底色淡染（与原按钮配色一致：普通项 FrameBg 系, 激活项强调色系）
        var bg = held
            ? active ? SimplePalette.WithAlpha(SimplePalette.Accent, 0.22f) : SimplePalette.FrameBgActive
            : hovered
                ? active ? SimplePalette.WithAlpha(SimplePalette.Accent, 0.12f) : SimplePalette.FrameBgHovered
                : Vector4.Zero;
        if (bg.W > 0f && alpha > 0f)
            drawList.AddRectFilled(min, max, SimplePalette.ToU32(SimplePalette.WithAlpha(bg, bg.W * alpha)), 3f);

        // 文字垂直居中、左对齐（与原 ButtonTextAlign(0,0.5) + FramePadding.X=8 一致）
        var textSize = ImGui.CalcTextSize(text);
        var textPos = new Vector2(min.X + 8f, min.Y + (max.Y - min.Y - textSize.Y) * 0.5f);
        drawList.AddText(textPos,
            SimplePalette.ToU32(SimplePalette.WithAlpha(textColor, textColor.W * alpha)), text);

        // 激活项左侧 3px 血红竖条
        if (active)
        {
            var bar = SimplePalette.NavActiveText;
            drawList.AddRectFilled(min + new Vector2(0f, 3f), new Vector2(min.X + 3f, max.Y - 3f),
                SimplePalette.ToU32(SimplePalette.WithAlpha(bar, bar.W * alpha)), 1.5f);
        }
        return clicked;
    }
}
