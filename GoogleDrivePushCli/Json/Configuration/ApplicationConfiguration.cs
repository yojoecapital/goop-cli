using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Json.Configuration;

public class ApplicationConfiguration
{
    [JsonPropertyName("cache")]
    public CacheConfiguration Cache { get; set; } = new();
    [JsonPropertyName("auto_ignore_list")]
    public List<string> AutoIgnoreList { get; set; } = [
        Defaults.syncFolderFileName,
        Defaults.ignoreListFileName
    ];

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
        var configuration = new ApplicationConfiguration();
        var json = JsonSerializer.Serialize(
            configuration,
            ApplicationConfigurationJsonContext.Pretty.ApplicationConfiguration
        ) + Environment.NewLine;
        File.WriteAllText(Defaults.configurationJsonPath, json);
        ConsoleHelpers.Info($"Created default application configuration file at '{Defaults.configurationJsonPath}'.");
        return configuration;
    }
}
