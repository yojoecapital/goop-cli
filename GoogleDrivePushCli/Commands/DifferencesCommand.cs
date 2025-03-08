using System;
using System.Collections.Generic;
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

public class DifferencesCommand : Command
{
    public DifferencesCommand() : base("diff", "Displays the differences in the last modified times between local and remote files.")
    {
        AddOption(DefaultParameters.ignoreOption);
        AddOption(DefaultParameters.workingDirectoryOption);
        this.SetHandler(
            Handle,
            DefaultParameters.ignoreOption,
            DefaultParameters.workingDirectoryOption
        );
    }

    private static void Handle(string[] ignoredPatterns, string workingDirectory)
    {
        var syncFolder = SyncFolder.Read(workingDirectory);
        syncFolder.IgnoreList.AddAll(ignoredPatterns);
        var fileDifferences = new List<FileDifference>();
        var folderDifferences = new List<FolderDifference>();
        AggregateDifferences(
            syncFolder, new(),
            fileDifferences, folderDifferences,
            syncFolder.FolderId, 0
        );
        var grid = new Grid();
        grid.AddColumns(3);
        grid.AddRow(["[bold]LOCAL[/]", "[bold]REMOTE[/]", "[bold]PATH[/]"]);
        foreach (var fileDifference in fileDifferences)
        {
            grid.AddRow([
                fileDifference.LocalDateTime?.ToLocalTime().ToString() ?? string.Empty,
                fileDifference.RemoteDateTime?.ToLocalTime().ToString() ?? string.Empty,
                fileDifference.Path
            ]);
        }
        foreach (var folderDifference in folderDifferences)
        {
            grid.AddRow([
                folderDifference.ExistsLocally ? "-" : string.Empty,
                folderDifference.ExistsRemotely ? "-" : string.Empty,
                folderDifference.Path
            ]);
        }
        AnsiConsole.Write(grid);
    }

    private static void AggregateDifferences(
        SyncFolder syncFolder,
        Stack<RemoteFolder> history,
        List<FileDifference> fileDifferences,
        List<FolderDifference> folderDifferences,
        string remoteFolderId,
        int depth
    )
    {
        int maxDepth = Math.Min(syncFolder.Depth, ApplicationConfiguration.Instance.MaxDepth);
        if (depth >= maxDepth) return;

        var service = DataAccessService.Instance;
        var relativePath = Path.Join([.. history.Select(remoteFolder => remoteFolder.Name).Reverse()]);
        var fullPath = Path.Join(syncFolder.LocalDirectory, relativePath);
        service.GetRemoteFolder(remoteFolderId, out var remoteFiles, out var remoteFolders);
        var remoteItemNames = remoteFiles
            .Select(remoteFile => remoteFile.Name)
            .Concat(remoteFolders.Select(remoteFolder => remoteFolder.Name))
            .ToHashSet();

        // Handle files
        foreach (var remoteFile in remoteFiles)
        {
            var fileFullPath = Path.Join(fullPath, remoteFile.Name);
            var fileRelativePath = Path.Join(relativePath, remoteFile.Name);
            if (syncFolder.IgnoreList.ShouldIgnore(fileRelativePath))
            {
                ConsoleHelpers.Info($"Skipping remote file '{fileRelativePath}' ({remoteFile.Id}).");
                continue;
            }
            if (File.Exists(fileFullPath))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(fileFullPath);
                var difference = new FileDifference()
                {
                    LocalDateTime = lastWriteTime,
                    RemoteDateTime = remoteFile.ModifiedTime.ToUtcDateTime(),
                    Path = fileRelativePath
                };
                fileDifferences.Add(difference);
            }
            else
            {
                var difference = new FileDifference()
                {
                    LocalDateTime = null,
                    RemoteDateTime = remoteFile.ModifiedTime.ToUtcDateTime(),
                    Path = fileRelativePath
                };
                fileDifferences.Add(difference);
            }
        }
        foreach (string fileFullPath in Directory.GetFiles(fullPath))
        {
            var fileName = Path.GetFileName(fileFullPath);
            var fileRelativePath = Path.Join(relativePath, fileName);
            if (syncFolder.IgnoreList.ShouldIgnore(fileRelativePath))
            {
                ConsoleHelpers.Info($"Skipping local file '{fileRelativePath}'.");
                continue;
            }
            if (remoteItemNames.Contains(fileName)) continue;
            var lastWriteTime = File.GetLastWriteTimeUtc(fileFullPath);
            var difference = new FileDifference()
            {
                LocalDateTime = lastWriteTime,
                RemoteDateTime = null,
                Path = fileRelativePath
            };
            fileDifferences.Add(difference);
        }

        // Handle folders
        foreach (string folderFullPath in Directory.GetDirectories(fullPath))
        {
            var folderName = Path.GetFileName(folderFullPath);
            var folderRelativePath = Path.Join(relativePath, folderName);
            if (syncFolder.IgnoreList.ShouldIgnore(folderRelativePath))
            {
                ConsoleHelpers.Info($"Skipping local folder '{folderRelativePath}'.");
                continue;
            }
            if (remoteItemNames.Contains(folderName)) continue;
            var difference = new FolderDifference()
            {
                ExistsLocally = true,
                ExistsRemotely = false,
                Path = Path.Join(folderRelativePath, "**")
            };
            folderDifferences.Add(difference);
        }
        foreach (var remoteFolder in remoteFolders)
        {
            var folderFullPath = Path.Join(fullPath, remoteFolder.Name);
            var folderRelativePath = Path.Join(relativePath, remoteFolder.Name);
            if (syncFolder.IgnoreList.ShouldIgnore(folderRelativePath))
            {
                ConsoleHelpers.Info($"Skipping remote folder '{folderRelativePath}' ({remoteFolder.Id}).");
                continue;
            }
            if (!Directory.Exists(folderFullPath))
            {
                var difference = new FolderDifference()
                {
                    ExistsLocally = false,
                    ExistsRemotely = true,
                    Path = Path.Join(folderRelativePath, "**")
                };
                folderDifferences.Add(difference);
                continue;
            }

            // Tail end recursion
            history.Push(remoteFolder);
            AggregateDifferences(
                syncFolder,
                history,
                fileDifferences,
                folderDifferences,
                remoteFolder.Id,
                depth + 1
            );
            history.Pop();
        }
    }
}