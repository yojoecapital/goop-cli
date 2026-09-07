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

    [JsonPropertyName("auth")]
    public TokenRefreshConfiguration TokenRefreshConfiguration { get; set; } = new();

    [JsonPropertyName("default_depth")]
    public int DefaultDepth { get; set; } = 3;

    [JsonPropertyName("max_depth")]
    public int MaxDepth { get; set; } = 3;

    private static ApplicationConfiguration instance;
    public static ApplicationConfiguration Instance
    {
        get
        {
            instance ??= CreateConfiguration();
            return instance;
        }
    }

    public static void Reload() => instance = null;

    public string ToJson() => JsonSerializer.Serialize(this, ApplicationConfigurationJsonContext.Pretty.ApplicationConfiguration);

    public void Save()
    {
        Defaults.EnsureConfigurationDirectory();
        File.WriteAllText(Defaults.configurationJsonPath, ToJson() + Environment.NewLine);
    }

    private static ApplicationConfiguration CreateConfiguration()
    {
        Defaults.EnsureConfigurationDirectory();
        if (File.Exists(Defaults.configurationJsonPath))
        {
            try
            {
                return JsonSerializer.Deserialize(
                    File.ReadAllText(Defaults.configurationJsonPath),
                    ApplicationConfigurationJsonContext.Lenient.ApplicationConfiguration
                ) ?? new ApplicationConfiguration();
            }
            catch (JsonException exception)
            {
                throw new Exception($"The configuration file at '{Defaults.configurationJsonPath}' is not valid JSON: {exception.Message}");
            }
        }
        var configuration = new ApplicationConfiguration();
        configuration.Save();
        ConsoleHelpers.Info($"Created default application configuration file at '{Defaults.configurationJsonPath}'.");
        return configuration;
    }
}
