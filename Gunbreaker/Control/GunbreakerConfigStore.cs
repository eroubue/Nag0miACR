using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nag0mi.Gunbreaker.Control;

internal sealed class GunbreakerConfigStore(string path)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string? RecoveryBackup { get; private set; }

    // 上次 Load 读到的旧版（v2 及更早）配置中抽出的 UI 状态，由调用方一次性导入框架设置；
    // 当前版本文件不含 UI 字段时为 null。
    public GunbreakerLegacyUi? LegacyUi { get; private set; }

    public GunbreakerControlConfig Load(Func<GunbreakerControlConfig> createDefault)
    {
        LegacyUi = null;
        if (!File.Exists(path)) return createDefault();
        try
        {
            var text = File.ReadAllText(path);
            // v3 起 UI 状态（面板布局/QT 顺序/各模式 QT 默认值/悬浮条位置）移入框架
            // ACRConfig\Nag0mi\GNB.json；旧文件先抽出这部分供一次性迁移，再按新模型反序列化
            // （多余字段被忽略），版本号升为当前版本。
            if (PeekVersion(text) < GunbreakerControlConfig.CurrentVersion)
                LegacyUi = GunbreakerLegacyUi.TryParse(text);
            var config = JsonSerializer.Deserialize<GunbreakerControlConfig>(text, Options);
            if (config == null) throw new JsonException("Invalid Nag0mi configuration");
            config.Version = GunbreakerControlConfig.CurrentVersion;
            if (!IsValid(config)) throw new JsonException("Invalid Nag0mi configuration");
            return config;
        }
        catch (JsonException)
        {
            RecoveryBackup = path + $".corrupt-{DateTime.UtcNow:yyyyMMddTHHmmssfff}-{Guid.NewGuid():N}.bak";
            // If backup fails, propagate the IO error instead of overwriting the user's file.
            File.Copy(path, RecoveryBackup);
            LegacyUi = null;
            return createDefault();
        }
    }

    private static int PeekVersion(string text)
    {
        try
        {
            return System.Text.Json.Nodes.JsonNode.Parse(text)?["Version"]?.GetValue<int>() ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public void Save(GunbreakerControlConfig config)
    {
        if (!IsValid(config)) throw new InvalidDataException("Invalid Nag0mi configuration");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(config, Options));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static bool IsValid(GunbreakerControlConfig config)
    {
        if (config.Version != GunbreakerControlConfig.CurrentVersion || !Enum.IsDefined(config.CurrentMode))
            return false;
        foreach (var mode in Enum.GetValues<GunbreakerMode>())
        {
            var profile = config.Get(mode);
            if (profile?.Parameters == null || !profile.Parameters.IsValid()) return false;
        }
        return true;
    }
}
