// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Nag0mi.Common.Data;
using Nag0mi.Gunbreaker.Control;
using PromeRotation.Data;

namespace Nag0mi.Common.UI;

// 四键竖直控制条（自研下沉，职业无关）：状态(播放/暂停/停止) / 模式循环 / 自动攻击 / 设置。
// 竖向悬浮条 + 背景拖动 + 松手左右吸附（OverlayGeometry.Resolve/Capture），吸附后贴边侧平直、
// 上下端呈水滴凹弧融边（背景/边框全自绘，吸附状态间 0.15s 渐变形变）；
// 归一化位置拆成 吸附侧/相对X/相对Y 三个基础字段持久化到 Nag0miUISettings；PreDraw 里 1s 节流压制宿主自带面板。
internal sealed class Nag0miUIControlWindow : Window
{
    // 逻辑尺寸（100% 缩放）：56×198，四键各 36px，纵向间距 46px
    private static readonly Vector2 LogicalSize = new(56f, 198f);

    // 吸附融边参数：毛细爬升高度 / 汇入端帽弧角度 / 形变时长（毫秒）
    private const float MeniscusReach = 10f;
    private const float MeniscusJoinDeg = 35f;
    private const float MorphMs = 150f;

    private static readonly Vector4 BgColor = new(.065f, .075f, .095f, .98f);
    private static readonly Vector4 EdgeColor = new(.26f, .29f, .34f, .7f);

    private bool dragging;
    private Vector2 dragMouse;
    private Vector2 dragPosition;
    private Vector2 lastViewport;
    private Vector2 lastOrigin;
    private float lastScale;
    private float scale;

    // 位置落盘节流（拖动中每帧改内存值，松手或 400ms 间隔才写盘）
    private bool placementDirty;
    private long nextSave;

    // 融边形变动画：morph 0=胶囊 ↔ 1=吸附融边；morphSide 记录动画期间用于镜像的吸附侧
    private bool morphInit;
    private float morph;
    private float morphFrom;
    private float morphTarget;
    private SnapSide morphSide = SnapSide.Right;
    private long morphStart;

    public Vector2 PixelPosition { get; private set; }
    public Vector2 PixelSize { get; private set; }

    public OverlayPlacement Placement
    {
        get
        {
            var s = Nag0miUISettings.Instance;
            return new OverlayPlacement
            {
                Side = (SnapSide)Math.Clamp(s.控制条吸附侧, 0, 2),
                RelativeX = s.控制条相对X,
                RelativeY = s.控制条相对Y,
            };
        }
    }

    public Nag0miUIControlWindow()
        : base($"Nag0mi{Nag0miUIJobEnv.JobTag} 控制###Nag0miUI.Control",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoFocusOnAppearing)
    {
        IsOpen = true;
        RespectCloseHotkey = false;
        ShowCloseButton = false;
        AllowPinning = false;
        AllowClickthrough = false;
        AllowBackgroundBlur = false;
        DisableWindowSounds = true;
        ForceMainWindow = true;
        SizeCondition = ImGuiCond.Always;
        PositionCondition = ImGuiCond.Always;
    }

    public override void Update()
    {
        // 拖动结束后 400ms 节流落盘（松手时立即 flush）
        if (placementDirty && Environment.TickCount64 >= nextSave)
            SavePlacement();
    }

    private void SetPlacement(OverlayPlacement placement)
    {
        var s = Nag0miUISettings.Instance;
        s.控制条吸附侧 = (int)placement.Side;
        s.控制条相对X = placement.RelativeX;
        s.控制条相对Y = placement.RelativeY;
        placementDirty = true;
    }

    private void SavePlacement()
    {
        if (!placementDirty) return;
        placementDirty = false;
        nextSave = Environment.TickCount64 + 400;
        Nag0miUISettings.Instance.Save();
    }

    public override void PreDraw()
    {
        // 压制宿主自带 QT 面板（内部 1s 节流，只能在 UI 线程调用）
        Nag0miUIFramework.HidePrPanels();

        var viewport = ImGui.GetMainViewport();
        // Keep the entire four-button capsule visible even in an unusually small viewport.
        scale = MathF.Min(ImGuiHelpers.GlobalScale, MathF.Min(viewport.Size.X / LogicalSize.X, viewport.Size.Y / LogicalSize.Y));
        scale = MathF.Max(.01f, scale);
        PixelSize = LogicalSize * scale;
        Size = PixelSize / ImGuiHelpers.GlobalScale; // WindowSystem applies GlobalScale to Size.

        if (lastViewport != viewport.Size || lastOrigin != viewport.Pos || lastScale != scale)
            dragging = false;
        lastViewport = viewport.Size;
        lastOrigin = viewport.Pos;
        lastScale = scale;

        if (dragging)
        {
            // On focus/mouse loss ImGui can return (-FLT_MAX, -FLT_MAX); never save that position.
            var mouseValid = ImGui.IsMousePosValid();
            if (mouseValid)
                SetPlacement(OverlayGeometry.Capture(dragPosition + ImGui.GetMousePos() - dragMouse,
                    viewport.Pos, viewport.Size, PixelSize, ImGuiHelpers.GlobalScale));
            if (!mouseValid || !ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                dragging = false;
                SavePlacement();
            }
        }
        PixelPosition = OverlayGeometry.Resolve(Placement, viewport.Pos, viewport.Size, PixelSize);
        // ForceMainWindow makes WindowSystem add MainViewport.Pos to Window.Position.
        Position = PixelPosition - viewport.Pos;
        UpdateMorph(Placement.Side);

        // 背景与边框全自绘（Draw 里按吸附形态画轮廓）：原生背景/边框隐藏，圆角归零
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 18 * scale);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, BgColor with { W = 0 });
        ImGui.PushStyleColor(ImGuiCol.Border, EdgeColor with { W = 0 });
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(5);
    }

    public override void Draw()
    {
        ImGui.SetWindowFontScale(scale / ImGuiHelpers.GlobalScale);
        DrawBackground();
        var host = PromeSettings.Instance;
        var disabled = host.EnableAcr == AcrState.Off;
        var paused = ControlInteraction.IsPaused(disabled, host.EnableAcr == AcrState.Hold);
        var color = disabled ? SimplePalette.StateOff
            : paused ? SimplePalette.StateHold : SimplePalette.StateRunning;
        var icon = disabled ? FontAwesomeIcon.Stop : paused ? FontAwesomeIcon.Pause : FontAwesomeIcon.Play;
        var stateText = disabled ? "已关闭 · 左键开启" : paused ? "已暂停 · 左键继续" : "运行中 · 左键暂停";

        // 1. 状态键
        var stateClick = IconButton("State", icon, 12, color);
        var stateRightClick = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var buttonHovered = ImGui.IsItemHovered();
        if (stateRightClick || stateClick)
        {
            var command = ControlInteraction.Click(disabled, host.EnableAcr == AcrState.Hold, stateRightClick);
            host.EnableAcr = command switch
            {
                ControlCommand.Resume => AcrState.On,
                ControlCommand.Pause => AcrState.Hold,
                _ => AcrState.Off,
            };
        }
        Tooltip($"{stateText}\n右键关闭 ACR");

        // 2. 模式循环键（job 注入；未注入时不响应点击）
        ImGui.SetCursorPos(new Vector2(10, 58) * scale);
        var modeLabel = Nag0miUIJobEnv.CurrentModeLabel?.Invoke() ?? "模";
        if (Button(modeLabel + "###Mode", SimplePalette.ModeButtonColor(模式显示名())))
            Nag0miUIJobEnv.CycleMode?.Invoke();
        buttonHovered |= ImGui.IsItemHovered();
        Tooltip($"当前模式：{模式显示名()}\n左键循环切换模式\n每套配置独立保存");

        // 3. 自动攻击键（开=彩色高亮 / 关=暗化）
        var autoPull = host.AutoPull;
        if (IconButton("AutoPull", FontAwesomeIcon.Crosshairs, 104,
                autoPull ? SimplePalette.StateRunning : SimplePalette.TextDisabled))
            host.AutoPull = !autoPull;
        buttonHovered |= ImGui.IsItemHovered();
        Tooltip($"自动攻击：{(autoPull ? "已开启" : "已关闭")}\n左键切换");

        // 4. 设置键（贴条展开设置窗口）
        if (IconButton("Settings", FontAwesomeIcon.Cog, 150, new Vector4(.79f, .81f, .85f, 1)))
            Nag0miUIFramework.ToggleSettings();
        buttonHovered |= ImGui.IsItemHovered();
        Tooltip("打开／关闭完整设置");

        // 背景（非按钮处）按住左键拖动整个条
        if (ImGui.IsWindowHovered() && !buttonHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            dragging = true;
            dragMouse = ImGui.GetMousePos();
            dragPosition = PixelPosition;
        }
    }

    private static string 模式显示名()
    {
        var s = Nag0miUISettings.Instance;
        var names = Nag0miUIJobEnv.ModeNames;
        return s.ModeIndex >= 0 && s.ModeIndex < names.Length ? names[s.ModeIndex] : $"模式{s.ModeIndex}";
    }

    // 形变动画推进：吸附状态变化时以当前 morph 为起点，150ms smoothstep 渐变
    private void UpdateMorph(SnapSide side)
    {
        var target = side == SnapSide.None ? 0f : 1f;
        if (side != SnapSide.None) morphSide = side;
        if (!morphInit)
        {
            // 首帧直接对齐目标形态，避免进游戏时播一次多余形变
            morphInit = true;
            morph = morphFrom = morphTarget = target;
            morphStart = Environment.TickCount64;
            return;
        }
        if (target != morphTarget)
        {
            morphFrom = CurrentMorph();
            morphTarget = target;
            morphStart = Environment.TickCount64;
        }
        morph = CurrentMorph();
    }

    private float CurrentMorph()
    {
        var p = Math.Clamp((Environment.TickCount64 - morphStart) / MorphMs, 0f, 1f);
        var s = p * p * (3f - 2f * p);
        return morphFrom + (morphTarget - morphFrom) * s;
    }

    // 自绘背景：轮廓起点取贴边中点（形状对其星形可见），扇形填充 + 同点列描边
    private void DrawBackground()
    {
        var pts = BuildOutline(morphSide == SnapSide.Right, Math.Clamp(morph, 0f, 1f));
        var draw = ImGui.GetWindowDrawList();
        draw.PathClear();
        foreach (var p in pts) draw.PathLineTo(p);
        draw.PathFillConvex(ImGui.GetColorU32(BgColor));
        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(pts);
        draw.AddPolyline(ref span[0], span.Length, ImGui.GetColorU32(EdgeColor), ImDrawFlags.Closed,
            MathF.Max(1f, scale));
    }

    // 轮廓生成（贴边侧按左侧构造，mirror 时 x 镜像；t 0=胶囊 ↔ 1=水滴凹弧融边）：
    // 贴边侧平直接触并向端帽方向爬升 q，凹弧贝塞尔（竖直切线入、端帽弧切线出）汇入端帽弧，外侧保持半圆端。
    private List<Vector2> BuildOutline(bool mirror, float t)
    {
        var w = PixelSize.X;
        var h = PixelSize.Y;
        var r = w * .5f;
        var q = MeniscusReach * scale * t;
        var join = float.DegreesToRadians(MeniscusJoinDeg) * t;
        var pts = new List<Vector2>(64);

        float X(float x) => mirror ? w - x : x;
        void Add(float x, float y) => pts.Add(PixelPosition + new Vector2(X(x), y));

        var ky = r - q;                             // 上接触点 K
        var jx = r - r * MathF.Cos(join);           // 汇入点 J（端帽弧 join 角处）
        var jy = r - r * MathF.Sin(join);
        var chord = MathF.Sqrt(jx * jx + (ky - jy) * (ky - jy));
        var handle = .55f * chord;

        // 贴边中点 → 沿边上行至 K → 凹弧贝塞尔 K→J
        Add(0, h * .5f);
        Add(0, ky);
        if (chord > .01f)
        {
            var k = new Vector2(0, ky);
            var j = new Vector2(jx, jy);
            var c1 = new Vector2(0, ky - handle);
            var c2 = new Vector2(jx - MathF.Sin(join) * handle, jy + MathF.Cos(join) * handle);
            for (var i = 1; i <= 8; i++) { var p = Cubic(k, c1, c2, j, i / 8f); Add(p.X, p.Y); }
        }
        else Add(jx, jy);

        // 上端帽弧（join → 90°）→ 顶边
        for (var i = 1; i <= 8; i++)
        {
            var a = join + (MathF.PI / 2f - join) * (i / 8f);
            Add(r - r * MathF.Cos(a), r - r * MathF.Sin(a));
        }
        Add(w - r, 0);

        // 右上弧 → 右边
        for (var i = 1; i <= 8; i++)
        {
            var b = MathF.PI / 2f * (i / 8f);
            Add(w - r + r * MathF.Sin(b), r - r * MathF.Cos(b));
        }
        Add(w, h - r);

        // 右下弧 → 底边
        for (var i = 1; i <= 8; i++)
        {
            var b = MathF.PI / 2f * (i / 8f);
            Add(w - r + r * MathF.Cos(b), h - r + r * MathF.Sin(b));
        }
        Add(r, h);

        // 下端帽弧（90° → join）→ 凹弧贝塞尔 J→K（竖直切线入边，与上半镜像）
        for (var i = 1; i <= 8; i++)
        {
            var a = MathF.PI / 2f - (MathF.PI / 2f - join) * (i / 8f);
            Add(r - r * MathF.Cos(a), h - r + r * MathF.Sin(a));
        }
        var kby = h - r + q;
        if (chord > .01f)
        {
            var j = new Vector2(jx, h - r + r * MathF.Sin(join));
            var k = new Vector2(0, kby);
            var c1 = j + new Vector2(-MathF.Sin(join), -MathF.Cos(join)) * handle;
            var c2 = new Vector2(0, kby + handle);
            for (var i = 1; i <= 8; i++) { var p = Cubic(j, c1, c2, k, i / 8f); Add(p.X, p.Y); }
        }
        else Add(0, kby);

        return pts;
    }

    private static Vector2 Cubic(Vector2 p0, Vector2 c1, Vector2 c2, Vector2 p3, float u)
    {
        var v = 1f - u;
        return v * v * v * p0 + 3f * v * v * u * c1 + 3f * v * u * u * c2 + u * u * u * p3;
    }

    private bool IconButton(string id, FontAwesomeIcon icon, float y, Vector4 color)
    {
        ImGui.SetCursorPos(new Vector2(10, y) * scale);
        ImGui.PushFont(UiBuilder.IconFont);
        try { return Button(icon.ToIconString() + "###" + id, color); }
        finally { ImGui.PopFont(); }
    }

    private bool Button(string label, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(color.X, color.Y, color.Z, .10f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(color.X, color.Y, color.Z, .24f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(color.X, color.Y, color.Z, .38f));
        try { return ImGui.Button(label, new Vector2(36) * scale); }
        finally { ImGui.PopStyleColor(4); }
    }

    private static void Tooltip(string text)
    {
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(text);
    }
}
