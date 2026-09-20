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
// 竖向悬浮条 + 背景拖动 + 松手左右吸附（OverlayGeometry.Resolve/Capture），
// 归一化位置拆成 吸附侧/相对X/相对Y 三个基础字段持久化到 Nag0miUISettings；PreDraw 里 1s 节流压制宿主自带面板。
internal sealed class Nag0miUIControlWindow : Window
{
    // 逻辑尺寸（100% 缩放）：56×224，四键各 36px，纵向间距 46px
    private static readonly Vector2 LogicalSize = new(56f, 224f);

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

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 28 * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1 * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 18 * scale);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(.065f, .075f, .095f, .98f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(.26f, .29f, .34f, .7f));
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(5);
    }

    public override void Draw()
    {
        ImGui.SetWindowFontScale(scale / ImGuiHelpers.GlobalScale);
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
        if (Button(modeLabel + "###Mode", SimplePalette.PrimaryHover))
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

        // 顶部握把
        var draw = ImGui.GetWindowDrawList();
        var grip = PixelPosition + new Vector2(20, 210) * scale;
        draw.AddLine(grip, grip + new Vector2(16, 0) * scale, ImGui.GetColorU32(new Vector4(.4f, .44f, .5f, .6f)), scale);

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
