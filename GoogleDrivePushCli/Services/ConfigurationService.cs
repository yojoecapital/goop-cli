// using System.IO;
// using System.Text.Json;
// using GoogleDrivePushCli.Data;

// namespace GoogleDrivePushCli.Services;

// public static class ConfigurationService
// {
//     private static Configuration configuration;

//     public static Configuration Configuration
//     {
//         get
//         {
//             configuration ??= CreateConfiguration();
//             return configuration;
//         }
//     }

//     private static Configuration CreateConfiguration()
//     {
//         if (File.Exists(Defaults.configurationJsonPath))
//         {
//             return JsonSerializer.Deserialize(
//                 File.ReadAllText(Defaults.configurationJsonPath),
//                 ConfigurationJsonContext.Default.Configuration
//             );
//         }
//         return new Configuration();
//     }
// }