using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using GoogleDrivePushCli.Json.Configuration;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands;

public class ConfigurationCommand : Command
{
    public ConfigurationCommand() : base("config", "Show or edit the application configuration.")
    {
        AddAlias("configuration");
        var pathOption = new Option<bool>("--path", "Print the path of the configuration file.");
        var editOption = new Option<bool>(["--edit", "-e"], "Open the configuration file in an editor.");
        var resetOption = new Option<bool>("--reset", "Restore the default configuration.");
        AddOption(pathOption);
        AddOption(editOption);
        AddOption(resetOption);
        AddOption(DefaultParameters.yesOption);
        this.SetHandler(Handle, pathOption, editOption, resetOption, DefaultParameters.yesOption);
    }

    private static void Handle(bool showPath, bool shouldEdit, bool shouldReset, bool skipConfirmation)
    {
        if (showPath)
        {
            Console.WriteLine(Defaults.configurationJsonPath);
            return;
        }
        if (shouldReset)
        {
            if (!skipConfirmation && !AnsiConsole.Confirm("Restore the default configuration?", false)) return;
            new ApplicationConfiguration().Save();
            ApplicationConfiguration.Reload();
            Console.WriteLine($"Restored the default configuration at '{Defaults.configurationJsonPath}'.");
            return;
        }
        if (shouldEdit)
        {
            OpenInEditor();
            return;
        }
        Console.WriteLine(ApplicationConfiguration.Instance.ToJson());
    }

    private static void OpenInEditor()
    {
        ApplicationConfiguration.Instance.Save();
        var editor = GetEditor();
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = editor,
                Arguments = $"\"{Defaults.configurationJsonPath}\"",
                UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            });
            process?.WaitForExit();
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to open '{Defaults.configurationJsonPath}' with '{editor}': {exception.Message}");
        }
    }

    private static string GetEditor()
    {
        var editor = Environment.GetEnvironmentVariable("VISUAL") ?? Environment.GetEnvironmentVariable("EDITOR");
        if (!string.IsNullOrWhiteSpace(editor)) return editor;
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "notepad" : "vi";
    }
}
