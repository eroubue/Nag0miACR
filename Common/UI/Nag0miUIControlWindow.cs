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
// 竖向悬浮条 + 背景拖动 + 松手左右吸附（OverlayGeometry.Resolve/Capture）；窗口逻辑尺寸固定 56×254，
// 悬浮时可见区为居中胶囊，吸附后贴边侧全高平直、上下端 S 曲线凹弧融入屏边
// （OverlayBerthShape 自绘轮廓，0.32s 微欠阻尼弹簧形变）；
// 归一化位置拆成 吸附侧/相对X/相对Y 三个基础字段持久化到 Nag0miUISettings；PreDraw 里 1s 节流压制宿主自带面板。
internal sealed class Nag0miUIControlWindow : Window
{
    // 逻辑尺寸（100% 缩放）：56×254，四键各 36px，纵向间距 46px；上下各 28 为融边预留（悬浮时透明）
    private static readonly Vector2 LogicalSize = new(OverlayBerthShape.Width, OverlayBerthShape.DockedHeight);

    // 墨锭形态：京元深色实体底 + 缟羽描边（不铺宣纸——S 曲线融边轮廓是凸多边形,
    // ImGui 不支持任意多边形贴图填充, 矩形裁切会在凹弧处穿帮; 墨锭配宣纸是完整的水墨语义）
    private static readonly Vector4 BgColor = ShuimoPalette.InkStick with { W = .98f };
    private static readonly Vector4 EdgeColor = ShuimoPalette.WithAlpha(ShuimoPalette.Hex(0xEEEEEE), .5f);

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

    // 融边形变动画：弹簧 0=胶囊 ↔ 1=吸附融边；morphSide 记录动画期间用于镜像的吸附侧
    private bool morphInit;
    private BerthSpring morph;
    private SnapSide morphSide = SnapSide.Right;
    private long morphTick;

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
        var placement = Placement;
        PixelPosition = OverlayGeometry.Resolve(placement, viewport.Pos, viewport.Size, PixelSize);
        // ForceMainWindow makes WindowSystem add MainViewport.Pos to Window.Position.
        Position = PixelPosition - viewport.Pos;
        UpdateMorph(placement.Side);

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
        var stateClick = IconButton("State", icon, OverlayBerthShape.ButtonRows[0], color);
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
        ImGui.SetCursorPos(new Vector2(10, OverlayBerthShape.ButtonRows[1]) * scale);
        var modeLabel = Nag0miUIJobEnv.CurrentModeLabel?.Invoke() ?? "模";
        if (Button(modeLabel + "###Mode", SimplePalette.ModeButtonColor(模式显示名())))
            Nag0miUIJobEnv.CycleMode?.Invoke();
        buttonHovered |= ImGui.IsItemHovered();
        Tooltip($"当前模式：{模式显示名()}\n左键循环切换模式\n每套配置独立保存");

        // 3. 自动攻击键（开=彩色高亮 / 关=缟羽压暗——墨锭深底上禁用灰/黑不可辨, 固定用压暗亮色）
        var autoPull = host.AutoPull;
        if (IconButton("AutoPull", FontAwesomeIcon.Crosshairs, OverlayBerthShape.ButtonRows[2],
                autoPull ? SimplePalette.StateRunning : ShuimoPalette.WithAlpha(ShuimoPalette.Hex(0xEEEEEE), 0.45f)))
            host.AutoPull = !autoPull;
        buttonHovered |= ImGui.IsItemHovered();
        Tooltip($"自动攻击：{(autoPull ? "已开启" : "已关闭")}\n左键切换");

        // 4. 设置键（贴条展开设置窗口; 缟羽图标）
        if (IconButton("Settings", FontAwesomeIcon.Cog, OverlayBerthShape.ButtonRows[3], ShuimoPalette.Hex(0xEEEEEE)))
            Nag0miUIFramework.ToggleSettings();
        buttonHovered |= ImGui.IsItemHovered();
        Tooltip("打开／关闭完整设置");

        // 背景（非按钮处）按住左键拖动整个条（上下融边预留的透明区不触发）
        if (ImGui.IsWindowHovered() && !buttonHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            var pad = OverlayBerthShape.VisiblePad(morph.Value) * scale;
            var localY = ImGui.GetMousePos().Y - PixelPosition.Y;
            if (localY >= pad && localY <= PixelSize.Y - pad)
            {
                dragging = true;
                dragMouse = ImGui.GetMousePos();
                dragPosition = PixelPosition;
            }
        }
    }

    private static string 模式显示名()
    {
        var s = Nag0miUISettings.Instance;
        var names = Nag0miUIJobEnv.ModeNames;
        return s.ModeIndex >= 0 && s.ModeIndex < names.Length ? names[s.ModeIndex] : $"模式{s.ModeIndex}";
    }

    // 形变动画推进：吸附状态作为弹簧目标（0.32s 微欠阻尼）；首帧直接落位，不播入场形变
    private void UpdateMorph(SnapSide side)
    {
        var now = Environment.TickCount64;
        var target = side == SnapSide.None ? 0f : 1f;
        if (side != SnapSide.None) morphSide = side;
        if (!morphInit)
        {
            morphInit = true;
            morph = new BerthSpring(target);
        }
        else
        {
            morph.Step(target, (now - morphTick) / 1000f);
        }
        morphTick = now;
    }

    // 自绘背景：轮廓对贴边中点星形可见（OverlayBerthShape 保证），PathFillConvex 扇形填充 + 同点列描边
    private void DrawBackground()
    {
        var pts = OverlayBerthShape.BuildOutline(morph.Value, morphSide == SnapSide.Right, scale, PixelPosition);
        var draw = ImGui.GetWindowDrawList();
        draw.PathClear();
        foreach (var p in pts) draw.PathLineTo(p);
        draw.PathFillConvex(ImGui.GetColorU32(BgColor));
        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(pts);
        draw.AddPolyline(ref span[0], span.Length, ImGui.GetColorU32(EdgeColor), ImDrawFlags.Closed,
            MathF.Max(1f, scale));
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
