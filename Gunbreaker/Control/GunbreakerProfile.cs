using System.Text.Json.Serialization;
using Nag0mi.Gunbreaker.Data;

namespace Nag0mi.Gunbreaker.Control;

internal enum GunbreakerMode { Normal, HighEnd, Custom }

internal sealed record GunbreakerParameters
{
    [JsonRequired] public bool Debug { get; init; }
    [JsonRequired] public bool SingleTarget { get; init; } = true;
    [JsonRequired] public int ReservedAmmo { get; init; }
    [JsonRequired] public GunbreakerSettings.工作模式枚举 WorkMode { get; init; }
    [JsonRequired] public GunbreakerSettings.起手方式枚举 PullMethod { get; init; }
    [JsonRequired] public int PullAdvanceMs { get; init; } = 500;
    [JsonRequired] public GunbreakerSettings.起手选择枚举 Opener { get; init; }
    // 非 JsonRequired：v3 旧配置无此键，缺省回落默认值 true（Required 会把旧配置判损坏并重置）
    public bool DashOpener { get; init; } = true;

    public static GunbreakerParameters Capture(GunbreakerSettings source) => new()
    {
        Debug = source.debug, SingleTarget = source.ST,
        ReservedAmmo = source.保留子弹数, WorkMode = source.当前工作模式,
        PullMethod = source.开怪方式, PullAdvanceMs = source.开怪提前时间, Opener = source.起手选择,
        DashOpener = source.突进起手,
    };

    public void Apply(GunbreakerSettings target)
    {
        target.debug = Debug;
        target.ST = SingleTarget;
        target.保留子弹数 = ReservedAmmo;
        target.当前工作模式 = WorkMode;
        target.开怪方式 = PullMethod;
        target.开怪提前时间 = PullAdvanceMs;
        target.起手选择 = Opener;
        target.突进起手 = DashOpener;
    }

    public bool IsValid() => ReservedAmmo is >= 0 and <= 3
        && PullAdvanceMs is >= 0 and <= 3000 && Enum.IsDefined(WorkMode)
        && Enum.IsDefined(PullMethod) && Enum.IsDefined(Opener);
}

internal sealed class GunbreakerProfile
{
    [JsonRequired] public GunbreakerParameters Parameters { get; set; } = new();
}

// 循环配置存档：只保留循环参数（三模式 profile）与行为开关。
// 悬浮条位置 / QT 面板布局 / QT 顺序 / 各模式 QT 默认值等 UI 状态
// 由框架 ACRConfig\Nag0mi\GNB.json 持有（见 GunbreakerLegacyUi 一次性迁移）。
internal sealed class GunbreakerControlConfig
{
    public const int CurrentVersion = 3;
    [JsonRequired] public int Version { get; set; } = CurrentVersion;
    [JsonRequired] public GunbreakerMode CurrentMode { get; set; } = GunbreakerMode.HighEnd;
    [JsonRequired] public GunbreakerProfile Normal { get; set; } = new();
    [JsonRequired] public GunbreakerProfile HighEnd { get; set; } = new();
    [JsonRequired] public GunbreakerProfile Custom { get; set; } = new();
    public bool ResetQtOnDeath { get; set; }

    public GunbreakerProfile Get(GunbreakerMode mode) => mode switch
    {
        GunbreakerMode.Normal => Normal, GunbreakerMode.HighEnd => HighEnd,
        GunbreakerMode.Custom => Custom, _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };
}
