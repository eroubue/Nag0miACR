using Nag0mi.Gunbreaker.Data;
using PromeRotation.Timeline.Core;

namespace Nag0mi.Gunbreaker.Timeline;

/// <summary>
/// 绝枪/设置：时间轴修改绝枪战斗设置（保留子弹数 / MT-ST / 起手选择 / 开怪方式与提前时间）。
/// 修改写入 GunbreakerSettings 单例，由配置窗口的自动同步落盘。
/// </summary>
public sealed class GunbreakerSettingsAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    internal const string TypeKey = "gnbsettings";

    private const string ReservedAmmoKey = "reserved_ammo";
    private const string SingleTargetKey = "st";
    private const string OpenerKey = "opener";
    private const string PullMethodKey = "pull_method";
    private const string PullAdvanceKey = "pull_advance";

    private static readonly (string Value, string Label)[] OpenerOptions =
    {
        (nameof(GunbreakerSettings.起手选择枚举.妖星起手), "妖星起手"),
        (nameof(GunbreakerSettings.起手选择枚举.无情2g起手), "无情2g起手"),
    };

    private static readonly (string Value, string Label)[] PullMethodOptions =
    {
        (nameof(GunbreakerSettings.起手方式枚举.关闭), "关闭"),
        (nameof(GunbreakerSettings.起手方式枚举.突进), "突进"),
        (nameof(GunbreakerSettings.起手方式枚举.闪雷弹), "闪雷弹"),
    };

    private int _reservedAmmo;
    private bool _singleTarget;
    private string _opener;
    private string _pullMethod;
    private int _pullAdvance;

    public GunbreakerSettingsAction()
    {
        var settings = GunbreakerSettings.Instance;
        _reservedAmmo = settings.保留子弹数;
        _singleTarget = settings.ST;
        _opener = settings.起手选择.ToString();
        _pullMethod = settings.开怪方式.ToString();
        _pullAdvance = settings.开怪提前时间;
    }

    public string NodeDisplayName => "绝枪/设置";

    public NodeParamInfo[] Params => new NodeParamInfo[]
    {
        new(ReservedAmmoKey, "保留子弹数", "0-3，执行时写入设置", "int"),
        new(SingleTargetKey, "单体模式(ST)", "勾选 = ST，不勾选 = MT", "bool"),
        new(OpenerKey, "起手选择", "设置当前起手方案", "enum", OpenerOptions),
        new(PullMethodKey, "开怪方式", "设置开怪方式", "enum", PullMethodOptions),
        new(PullAdvanceKey, "开怪提前时间(ms)", "设置开怪提前时间", "int"),
    };

    public string GetParam(string fieldName) => fieldName switch
    {
        ReservedAmmoKey => _reservedAmmo.ToString(),
        SingleTargetKey => _singleTarget.ToString(),
        OpenerKey => _opener,
        PullMethodKey => _pullMethod,
        PullAdvanceKey => _pullAdvance.ToString(),
        _ => "",
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case ReservedAmmoKey:
                if (int.TryParse(value, out var reserved)) _reservedAmmo = Math.Clamp(reserved, 0, 3);
                break;
            case SingleTargetKey:
                _singleTarget = bool.TryParse(value, out var st) && st;
                break;
            case OpenerKey:
                if (Enum.TryParse<GunbreakerSettings.起手选择枚举>(value, out var opener)) _opener = opener.ToString();
                break;
            case PullMethodKey:
                if (Enum.TryParse<GunbreakerSettings.起手方式枚举>(value, out var pull)) _pullMethod = pull.ToString();
                break;
            case PullAdvanceKey:
                if (int.TryParse(value, out var advance)) _pullAdvance = Math.Clamp(advance, 0, 3000);
                break;
        }
    }

    public void Execute()
    {
        var settings = GunbreakerSettings.Instance;
        settings.保留子弹数 = _reservedAmmo;
        settings.ST = _singleTarget;
        settings.开怪提前时间 = _pullAdvance;

        if (Enum.TryParse<GunbreakerSettings.起手选择枚举>(_opener, out var opener))
            settings.起手选择 = opener;

        if (Enum.TryParse<GunbreakerSettings.起手方式枚举>(_pullMethod, out var pull))
            settings.开怪方式 = pull;
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string>
        {
            [ReservedAmmoKey] = _reservedAmmo.ToString(),
            [SingleTargetKey] = _singleTarget.ToString(),
            [OpenerKey] = _opener,
            [PullMethodKey] = _pullMethod,
            [PullAdvanceKey] = _pullAdvance.ToString(),
        },
    };

    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, TypeKey, FromDto);
    }

    private static GunbreakerSettingsAction FromDto(ActionDto dto)
    {
        var action = new GunbreakerSettingsAction();
        if (dto.Params == null)
            return action;

        if (dto.Params.TryGetValue(ReservedAmmoKey, out var reserved)) action.SetParam(ReservedAmmoKey, reserved);
        if (dto.Params.TryGetValue(SingleTargetKey, out var st)) action.SetParam(SingleTargetKey, st);
        if (dto.Params.TryGetValue(OpenerKey, out var opener)) action.SetParam(OpenerKey, opener);
        if (dto.Params.TryGetValue(PullMethodKey, out var pull)) action.SetParam(PullMethodKey, pull);
        if (dto.Params.TryGetValue(PullAdvanceKey, out var advance)) action.SetParam(PullAdvanceKey, advance);

        return action;
    }
}
