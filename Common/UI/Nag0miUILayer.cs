// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Nag0mi.Common.UI;

// 自绘背景窗口共用的绘制占位辅助。
// 背景必须画进窗口自己的绘制列表，窗口叠加顺序才是对的：画进 viewport 背景层
// 会让背景沉到所有窗口之下，两个窗口重叠时内容会互相穿透。
// 但窗口开启背景模糊时，Dalamud 的 PrependBlurBehind 会替换掉窗口绘制列表的
// 第一条命令。所以自绘背景前先垫一条零面积矩形占住第一位，真正的绘制从第二条开始。
internal static class Nag0miUILayer
{
    // 零面积全透明矩形：只占住绘制列表第一位的命令槽，不产生任何视觉
    public static void 垫牺牲帧(ImDrawListPtr drawList) =>
        drawList.AddRectFilled(Vector2.Zero, Vector2.Zero, 0u);
}
