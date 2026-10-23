using Nag0mi.Gunbreaker.Control;
using Nag0mi.Gunbreaker.Data;

namespace Nag0mi.Tests;

internal static class ProfileTests
{
    public static void Run()
    {
        CapturePreservesUnchangedSnapshot();
        CaptureDetectsEachParameter();

        foreach (var disabled in new[] { false, true })
        foreach (var held in new[] { false, true })
        {
            Check.Equal(ControlCommand.Close, ControlInteraction.Click(disabled, held, true));
            Check.Equal(disabled || held ? ControlCommand.Resume : ControlCommand.Pause,
                ControlInteraction.Click(disabled, held, false));
            Check.Equal(!disabled && held, ControlInteraction.IsPaused(disabled, held));
        }
        Console.WriteLine("PASS: all host state click combinations");

        var settings = new GunbreakerSettings { ST = false, debug = true, 保留子弹数 = 2,
            开怪提前时间 = 1700, 开怪方式 = GunbreakerSettings.起手方式枚举.闪雷弹,
            起手选择 = GunbreakerSettings.起手选择枚举.无情2g起手,
            当前工作模式 = GunbreakerSettings.工作模式枚举.日常模式, 突进起手 = false };
        var profiles = GunbreakerProfiles.Create(settings);
        Check.Equal(GunbreakerMode.HighEnd, profiles.Config.CurrentMode);
        Check.Equal(0, profiles.Config.Normal.Parameters.ReservedAmmo);
        Check.Equal(profiles.Config.Normal.Parameters, profiles.Config.HighEnd.Parameters);
        Check.Equal(2, profiles.Config.Custom.Parameters.ReservedAmmo);
        Check.Equal(GunbreakerSettings.起手选择枚举.无情2g起手, profiles.Config.Custom.Parameters.Opener);

        profiles.Apply(settings);
        Check.Equal(0, settings.保留子弹数); // HighEnd keeps the factory parameters.
        Check.True(settings.突进起手); // factory default is on

        settings.保留子弹数 = 1;
        profiles.Switch(GunbreakerMode.Custom, settings);
        Check.Equal(1, profiles.Config.HighEnd.Parameters.ReservedAmmo); // captured on switch away
        Check.Equal(2, settings.保留子弹数); // Custom parameters applied
        Check.True(settings.debug && !settings.ST);
        Check.True(!settings.突进起手); // Custom parameters applied
        Check.Equal(1700, settings.开怪提前时间);
        Check.Equal(GunbreakerSettings.起手方式枚举.闪雷弹, settings.开怪方式);
        Check.Equal(GunbreakerSettings.起手选择枚举.无情2g起手, settings.起手选择);
        Check.Equal(GunbreakerSettings.工作模式枚举.日常模式, settings.当前工作模式);

        settings.保留子弹数 = 3;
        Check.True(profiles.Capture(settings));
        Check.True(!profiles.Capture(settings)); // unchanged parameters never dirty the config
        Check.Equal(3, profiles.Config.Custom.Parameters.ReservedAmmo);
        Check.Equal(0, profiles.Config.Normal.Parameters.ReservedAmmo);
        Check.Equal(GunbreakerMode.Normal, GunbreakerProfiles.Next(GunbreakerMode.Custom));
        Check.Equal(GunbreakerMode.HighEnd, GunbreakerProfiles.Next(GunbreakerMode.Normal));
        Check.Equal(GunbreakerMode.Custom, GunbreakerProfiles.Next(GunbreakerMode.HighEnd));

        profiles.Switch(GunbreakerMode.Normal, settings);
        settings.保留子弹数 = 2;
        profiles.Switch(GunbreakerMode.HighEnd, settings);
        Check.Equal(1, settings.保留子弹数);
        Check.Equal(2, profiles.Config.Normal.Parameters.ReservedAmmo);
        profiles.Switch(GunbreakerMode.Custom, settings);
        Check.Equal(3, settings.保留子弹数);
        Check.Throws<ArgumentOutOfRangeException>(() => profiles.Switch((GunbreakerMode)99, settings));
        Console.WriteLine("PASS: parameter capture/apply, mode isolation, switching");

        var directory = Path.Combine(AppContext.BaseDirectory, "config-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "Nag0mi.Gunbreaker.json");
            var store = new GunbreakerConfigStore(path);
            profiles.Config.ResetQtOnDeath = true;
            store.Save(profiles.Config);
            Check.Equal(null, store.LegacyUi); // Save never produces legacy UI data.
            var loaded = store.Load(() => throw new Exception("Unexpected fallback"));
            Check.Equal(GunbreakerMode.Custom, loaded.CurrentMode);
            Check.Equal(3, loaded.Custom.Parameters.ReservedAmmo);
            Check.Equal(1, loaded.HighEnd.Parameters.ReservedAmmo);
            Check.Equal(2, loaded.Normal.Parameters.ReservedAmmo);
            Check.True(loaded.ResetQtOnDeath);
            Check.True(!loaded.Custom.Parameters.DashOpener);
            var json = File.ReadAllText(path);
            Check.True(!json.Contains("EnableAcr") && !json.Contains("IsOpen")
                && !json.Contains("QuickToggles") && !json.Contains("QtPanel")
                && !json.Contains("HotkeyPanel") && !json.Contains("Window"));
            Check.Equal(null, store.LegacyUi); // Current-version files carry no legacy UI data.
            loaded.CurrentMode = GunbreakerMode.Normal;
            store.Save(loaded); // Replace existing file atomically.
            Check.Equal(GunbreakerMode.Normal, store.Load(() => throw new Exception()).CurrentMode);
            Check.Equal(0, Directory.GetFiles(directory, "*.tmp").Length);

            // v2 → v3 migration: rotation parameters and the death-reset flag survive,
            // UI state (panel layout / QT order / per-mode QT defaults / bar placement)
            // is extracted for the one-time import into the framework settings file.
            // The AoeCount field removed from current parameters is simply ignored on load.
            var v2 = $$"""
            {
              "Version": 2,
              "CurrentMode": "HighEnd",
              "Normal": {
                "Parameters": { "Debug": false, "SingleTarget": true, "AoeCount": 2, "ReservedAmmo": 0,
                                "WorkMode": "日常模式", "PullMethod": "关闭", "PullAdvanceMs": 500, "Opener": "妖星起手" },
                "QuickToggles": { "无情": true, "爆发": false, "其他职业QT": true }
              },
              "HighEnd": {
                "Parameters": { "Debug": false, "SingleTarget": true, "AoeCount": 3, "ReservedAmmo": 1,
                                "WorkMode": "高难模式", "PullMethod": "突进", "PullAdvanceMs": 800, "Opener": "绝欧起手" },
                "QuickToggles": { "无情": false }
              },
              "Custom": {
                "Parameters": { "Debug": true, "SingleTarget": false, "AoeCount": 5, "ReservedAmmo": 2,
                                "WorkMode": "日常模式", "PullMethod": "闪雷弹", "PullAdvanceMs": 1200, "Opener": "巴哈起手" },
                "QuickToggles": {}
              },
              "Window": { "Side": "Left", "RelativeX": 0, "RelativeY": 0.73 },
              "QtPanel": { "Columns": 8, "Spacing": 12.5, "Scale": 1.5, "LockOrder": true,
                           "Order": ["爆发", "无情", "未知QT", "无情"] },
              "HotkeyPanel": { "Columns": 6, "ButtonSize": 60, "Spacing": 8 },
              "ResetQtOnDeath": true
            }
            """;
            File.WriteAllText(path, v2);
            var migratedStore = new GunbreakerConfigStore(path);
            var migrated = migratedStore.Load(() => throw new Exception("Unexpected fallback"));
            Check.Equal(GunbreakerControlConfig.CurrentVersion, migrated.Version);
            Check.Equal(GunbreakerMode.HighEnd, migrated.CurrentMode);
            Check.Equal(1, migrated.HighEnd.Parameters.ReservedAmmo);
            Check.Equal(GunbreakerSettings.起手方式枚举.突进, migrated.HighEnd.Parameters.PullMethod);
            Check.Equal(2, migrated.Custom.Parameters.ReservedAmmo);
            Check.True(migrated.ResetQtOnDeath);
            Check.True(migrated.HighEnd.Parameters.DashOpener); // v2 无此键，缺省 true

            var legacy = migratedStore.LegacyUi!;
            Check.True(legacy != null);
            Check.Equal(8, legacy!.QtPanelColumns!.Value);
            Check.Near(12.5f, legacy.QtPanelSpacing!.Value);
            Check.Near(1.5f, legacy.QtPanelScale!.Value);
            Check.True(legacy.QtPanelLockOrder);
            Check.Equal(2, legacy.QtOrder.Count); // unknown/duplicate keys dropped
            Check.Equal("爆发", legacy.QtOrder[0]);
            Check.Equal("无情", legacy.QtOrder[1]);
            Check.Equal(6, legacy.HotkeyColumns!.Value);
            Check.Near(60f, legacy.HotkeyButtonSize!.Value);
            Check.Near(8f, legacy.HotkeySpacing!.Value);
            Check.Equal(1, legacy.WindowSide!.Value); // Left
            Check.Near(.73f, legacy.WindowRelativeY);
            Check.Equal(2, legacy.QtDefaultsByMode.Count); // empty Custom dict skipped
            Check.True(legacy.QtDefaultsByMode[0]["无情"]);
            Check.True(!legacy.QtDefaultsByMode[0].ContainsKey("其他职业QT"));
            Check.True(!legacy.QtDefaultsByMode[1]["无情"]);

            // v1 files carry no UI fields at all: still load, with nothing to migrate.
            var v1 = System.Text.Json.Nodes.JsonNode.Parse(v2)!;
            v1["Version"] = 1;
            v1.AsObject().Remove("QtPanel");
            v1.AsObject().Remove("HotkeyPanel");
            v1.AsObject().Remove("Window");
            v1.AsObject().Remove("ResetQtOnDeath");
            File.WriteAllText(path, v1.ToJsonString());
            var v1Store = new GunbreakerConfigStore(path);
            var fromV1 = v1Store.Load(() => throw new Exception("Unexpected fallback"));
            Check.Equal(1, fromV1.HighEnd.Parameters.ReservedAmmo);
            Check.True(!fromV1.ResetQtOnDeath);
            Check.True(v1Store.LegacyUi != null); // per-mode QT defaults still migrate
            Check.Equal(null, v1Store.LegacyUi!.QtPanelColumns);
            Check.Equal(null, v1Store.LegacyUi.WindowSide);

            foreach (var broken in new[] { "{broken", "null", "{}",
                         v2.Replace("\"ReservedAmmo\": 1", "\"ReservedAmmo\": 9") })
            {
                File.WriteAllText(path, broken);
                var recovered = store.Load(() => GunbreakerProfiles.Create(new GunbreakerSettings()).Config);
                Check.Equal(GunbreakerMode.HighEnd, recovered.CurrentMode);
                Check.True(Directory.GetFiles(directory, "*.corrupt-*.bak").Any(p => File.ReadAllText(p) == broken));
            }
            Console.WriteLine("PASS: save/reload, v1/v2 migration with legacy UI extraction, invalid JSON/values backed up and recovered");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void CapturePreservesUnchangedSnapshot()
    {
        var settings = new GunbreakerSettings();
        var profiles = GunbreakerProfiles.Create(settings);
        var snapshot = profiles.Config.HighEnd.Parameters;
        profiles.Capture(settings);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 256; i++)
        {
            Check.True(!profiles.Capture(settings));
            Check.True(ReferenceEquals(snapshot, profiles.Config.HighEnd.Parameters),
                "Unchanged parameters must retain the same snapshot instance");
        }
        Check.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
        Console.WriteLine("PASS: unchanged parameter capture retains snapshot reference");
    }

    private static void CaptureDetectsEachParameter()
    {
        (string Name, Action<GunbreakerSettings> Change)[] changes =
        [
            (nameof(GunbreakerParameters.Debug), s => s.debug = true),
            (nameof(GunbreakerParameters.SingleTarget), s => s.ST = false),
            (nameof(GunbreakerParameters.ReservedAmmo), s => s.保留子弹数 = 2),
            (nameof(GunbreakerParameters.WorkMode), s => s.当前工作模式 = GunbreakerSettings.工作模式枚举.日常模式),
            (nameof(GunbreakerParameters.PullMethod), s => s.开怪方式 = GunbreakerSettings.起手方式枚举.闪雷弹),
            (nameof(GunbreakerParameters.PullAdvanceMs), s => s.开怪提前时间 = 1700),
            (nameof(GunbreakerParameters.Opener), s => s.起手选择 = GunbreakerSettings.起手选择枚举.无情2g起手),
            (nameof(GunbreakerParameters.DashOpener), s => s.突进起手 = false),
        ];
        foreach (var (name, change) in changes)
        {
            var settings = new GunbreakerSettings();
            var profiles = GunbreakerProfiles.Create(settings);
            var original = profiles.Config.HighEnd.Parameters;
            change(settings);
            Check.True(profiles.Capture(settings), $"Changing {name} must dirty the profile");
            var changed = profiles.Config.HighEnd.Parameters;
            Check.True(!ReferenceEquals(original, changed), $"Changing {name} must replace the snapshot");
            Check.Equal(GunbreakerParameters.Capture(settings), changed);
            Check.True(!profiles.Capture(settings));
            Check.True(ReferenceEquals(changed, profiles.Config.HighEnd.Parameters));
            original.Apply(settings);
            Check.True(profiles.Capture(settings), $"Restoring {name} must dirty the profile");
            Check.Equal(original, profiles.Config.HighEnd.Parameters);
        }
        Console.WriteLine("PASS: each parameter change and restoration updates the snapshot");
    }
}
