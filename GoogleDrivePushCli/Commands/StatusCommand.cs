using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using GoogleDrivePushCli.Json.Configuration;
using GoogleDrivePushCli.Json.SyncFolder;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands;

public class StatusCommand : Command
{
    public StatusCommand() : base("status", "Show information about the current sync folder.")
    {
        AddAlias("st");
        AddOption(DefaultParameters.workingDirectoryOption);
        this.SetHandler(Handle, DefaultParameters.workingDirectoryOption);
    }

    private static void Handle(string workingDirectory)
    {
        var syncFolder = SyncFolder.Read(workingDirectory);
        var remoteItems = DataAccessService.Instance.GetRemoteItemsFromPath("/", syncFolder.FolderId);
        var remoteFolder = remoteItems.Peek();
        var grid = new Grid();
        grid.AddColumns(2);
        AddRow(grid, "Local directory", syncFolder.LocalDirectory);
        AddRow(grid, "Remote folder", remoteFolder.Name);
        AddRow(grid, "Remote folder ID", syncFolder.FolderId);
        AddRow(grid, "Depth", $"{syncFolder.Depth} (effective {syncFolder.EffectiveDepth})");
        AddRow(grid, "Cache", ApplicationConfiguration.Instance.Cache.Enabled ? "enabled" : "disabled");
        AddRow(grid, "Cache database", File.Exists(Defaults.cacheDatabasePath) ? Defaults.cacheDatabasePath : "not created");
        AddRow(grid, "Configuration", Defaults.configurationJsonPath);
        AddRow(grid, "Ignore file", IgnoreFileStatus(syncFolder));
        AnsiConsole.Write(grid);
    }

    private static string IgnoreFileStatus(SyncFolder syncFolder)
    {
        var ignoreFilePath = Path.Join(syncFolder.LocalDirectory, Defaults.ignoreListFileName);
        if (!File.Exists(ignoreFilePath)) return "none";
        var patternCount = File.ReadLines(ignoreFilePath).Count(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith('#'));
        return $"{ignoreFilePath} ({patternCount} pattern(s))";
    }

    private static void AddRow(Grid grid, string label, string value) =>
        grid.AddRow([$"[bold]{label}[/]", $": {value.EscapeMarkup()}"]);
}
