using Nag0mi.Gunbreaker.Data;

namespace Nag0mi.Gunbreaker.Control;

internal sealed class GunbreakerProfiles(GunbreakerControlConfig config)
{
    public GunbreakerControlConfig Config { get; } = config;

    public static GunbreakerProfiles Create(GunbreakerSettings current)
    {
        var defaults = GunbreakerParameters.Capture(new GunbreakerSettings());
        return new(new()
        {
            Normal = new() { Parameters = defaults },
            HighEnd = new() { Parameters = defaults with { } },
            Custom = new() { Parameters = GunbreakerParameters.Capture(current) },
        });
    }

    public static GunbreakerMode Next(GunbreakerMode mode) => mode switch
    {
        GunbreakerMode.Normal => GunbreakerMode.HighEnd,
        GunbreakerMode.HighEnd => GunbreakerMode.Custom,
        _ => GunbreakerMode.Normal,
    };

    // Only parameters follow the live settings; QT defaults live in the framework
    // per-mode snapshots (Nag0miUISettings.QtDefaultsByMode).
    public bool Capture(GunbreakerSettings settings)
    {
        var profile = Config.Get(Config.CurrentMode);
        if (profile.Parameters.Matches(settings)) return false;
        profile.Parameters = GunbreakerParameters.Capture(settings);
        return true;
    }

    public void Apply(GunbreakerSettings settings) =>
        Config.Get(Config.CurrentMode).Parameters.Apply(settings);

    public void Switch(GunbreakerMode target, GunbreakerSettings settings)
    {
        if (!Enum.IsDefined(target)) throw new ArgumentOutOfRangeException(nameof(target));
        Capture(settings);
        Config.CurrentMode = target;
        Apply(settings);
    }
}
