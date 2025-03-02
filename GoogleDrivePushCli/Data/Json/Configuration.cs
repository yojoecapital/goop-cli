using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Data
{
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

    }
}