using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Json.Configuration;

public class ApplicationConfiguration
{
    [JsonPropertyName("cache")]
    public CacheConfiguration Cache { get; set; } = new();
    [JsonPropertyName("auto_ignore_list")]
    public List<string> AutoIgnoreList { get; set; } = [];

    [JsonPropertyName("default_depth")]
    public int DefaultDepth { get; set; } = 3;
    [JsonPropertyName("max_depth")]
    public int MaxDepth { get; set; } = 3;
    [JsonPropertyName("shortcut_template")]
    public string ShortcutTemplate { get; set; }

    private static ApplicationConfiguration instance;
    public static ApplicationConfiguration Instance
    {
        get
        {
            instance ??= CreateConfiguration();
            return instance;
        }
    }

    private static ApplicationConfiguration CreateConfiguration()
    {
        if (File.Exists(Defaults.configurationPath))
        {
            return JsonSerializer.Deserialize(
                File.ReadAllText(Defaults.configurationJsonPath),
                ApplicationConfigurationJsonContext.Default.ApplicationConfiguration
            );
        }
        return new ApplicationConfiguration();
    }
}
