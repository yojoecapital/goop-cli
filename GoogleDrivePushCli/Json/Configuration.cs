using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Json;

public class Configuration
{
    [JsonPropertyName("cache")]
    public CacheConfiguration Cache { get; set; } = new();
    [JsonPropertyName("auto_ignore_list")]
    public List<string> AutoIgnoreList { get; set; } = [];

    [JsonPropertyName("default_depth")]
    public long DefaultDepth { get; set; } = 3;
    [JsonPropertyName("shortcut_template")]
    public string ShortcutTemplate { get; set; }

    private static Configuration instance;
    public static Configuration Instance
    {
        get
        {
            instance ??= CreateConfiguration();
            return instance;
        }
    }

    private static Configuration CreateConfiguration()
    {
        if (File.Exists(Defaults.configurationPath))
        {
            return JsonSerializer.Deserialize(
                File.ReadAllText(Defaults.configurationJsonPath),
                ConfigurationJsonContext.Default.Configuration
            );
        }
        return new Configuration();
    }
}
