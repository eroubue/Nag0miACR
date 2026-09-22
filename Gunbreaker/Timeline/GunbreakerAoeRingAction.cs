using PromeRotation.Core;
using PromeRotation.Spatial.Drawing;
using PromeRotation.Spatial.Geometry;
using PromeRotation.Timeline.Core;

namespace Nag0mi.Gunbreaker.Timeline;

/// <summary>
/// 绝枪/5m圆环绘制：以玩家为圆心绘制内半径 4.9m、外半径 5.0m 的圆环（自身 AOE 范围参考线）。
/// 模式分开始绘制 / 关闭绘制；开始可重复执行（固定 ID 覆盖），脱战或换图时由宿主自动清除。
/// </summary>
public sealed class GunbreakerAoeRingAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    internal const string TypeKey = "gnbaoering";

    private const string DrawId = "Nag0mi_GNB_AoeRing5m";
    private const string ModeKey = "mode";

    private const float InnerRadius = 4.9f;
    private const float OuterRadius = 5.0f;

    private enum DrawMode
    {
        开始绘制,
        关闭绘制,
    }

    private static readonly (string Value, string Label)[] ModeOptions =
    {
        (nameof(DrawMode.开始绘制), "开始绘制"),
        (nameof(DrawMode.关闭绘制), "关闭绘制"),
    };

    private string _mode = nameof(DrawMode.开始绘制);

    public string NodeDisplayName => "绝枪/5m圆环绘制";

    public NodeParamInfo[] Params => new NodeParamInfo[]
    {
        new(ModeKey, "模式", "开始绘制 = 以玩家为圆心绘制 5m 圆环；关闭绘制 = 移除该圆环", "enum", ModeOptions),
    };

    public string GetParam(string fieldName) => fieldName == ModeKey ? _mode : "";

    public void SetParam(string fieldName, string value)
    {
        if (fieldName == ModeKey && Enum.TryParse<DrawMode>(value, out var mode))
            _mode = mode.ToString();
    }

    public void Execute()
    {
        if (_mode == nameof(DrawMode.关闭绘制))
        {
            PromeRotation.Plugin.Instance.DrawManager.Remove(DrawId);
            return;
        }

        var me = Core.Me;
        if (me == null)
            return;

        // 固定 ID：重复执行开始绘制时直接覆盖同一条目，圆环通过 Follow 绑定始终跟随玩家
        PromeRotation.Plugin.Instance.DrawManager.AddBound(
            DrawId,
            new DonutGeometry(me.Position, InnerRadius, OuterRadius),
            DrawBinding.Follow(me),
            DrawStyle.Default);
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string>
        {
            [ModeKey] = _mode,
        },
    };

    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, TypeKey, FromDto);
    }

    private static GunbreakerAoeRingAction FromDto(ActionDto dto)
    {
        var action = new GunbreakerAoeRingAction();
        if (dto.Params != null && dto.Params.TryGetValue(ModeKey, out var mode))
            action.SetParam(ModeKey, mode);

        return action;
    }
}
