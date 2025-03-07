using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using GoogleDrivePushCli.Json.SyncFolder;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands;

public class PullCommand : Command
{
    public PullCommand() : base("pull", "Pulls remote changes from Google Drive.")
    {
        AddOption(DefaultParameters.operationsOption);
        AddOption(DefaultParameters.workingDirectoryOption);
        this.SetHandler(
            Handle,
            DefaultParameters.operationsOption,
            DefaultParameters.workingDirectoryOption,
            DefaultParameters.yesOption
        );
    }

    private static void Handle(string operations, string workingDirectory, bool skipConfirmation)
    {
        var createOperations = new List<Operation>();
        var updateOperations = new List<Operation>();
        var deleteOperations = new List<Operation>();
        var syncFolder = SyncFolder.Read(workingDirectory);
        AggregateOperations(
            syncFolder,
            createOperations,
            updateOperations,
            deleteOperations,
            new(),
            syncFolder.FolderId,
            0
        );
        OperationHelpers.PromptAndRun(
            createOperations,
            updateOperations,
            deleteOperations,
            skipConfirmation
        );
    }

    private static void AggregateOperations(
        SyncFolder syncFolder,
        List<Operation> createOperations,
        List<Operation> updateOperations,
        List<Operation> deleteOperations,
        Stack<RemoteFolder> history,
        string remoteFolderId,
        int depth
    )
    {
        var service = DataAccessService.Instance;
        var relativePath = Path.Join([.. history.Select(remoteFolder => remoteFolder.Name)]);
        var fullPath = Path.Join(syncFolder.LocalDirectory, relativePath);
        DataAccessService.Instance.GetRemoteFolder(remoteFolderId, out var remoteFiles, out var remoteFolders);
        var remoteItemNames = remoteFiles
            .Select(remoteFile => remoteFile.Name)
            .Concat(remoteFolders.Select(remoteFolder => remoteFolder.Name))
            .ToHashSet();

        // Handle files
        foreach (var remoteFile in remoteFiles)
        {
            var fileFullPath = Path.Join(fullPath, remoteFile.Name);
            var fileRelativePath = Path.Join(relativePath, remoteFile.Name);
            if (syncFolder.IgnoreListService.ShouldIgnore(fileRelativePath))
            {
                ConsoleHelpers.Info($"Skipping remote file '{fileRelativePath}' ({remoteFile.Id}).");
                continue;
            }
            if (File.Exists(fileFullPath))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(fileFullPath);
                if (lastWriteTime >= remoteFile.ModifiedTime) continue;

                // File was edited
                var operation = new Operation(
                    $"Local file '{fileRelativePath}'.",
                    progress => DataAccessService.Instance.DownloadFile(remoteFile, fileFullPath, progress)
                );
                updateOperations.Add(operation);
            }
            else
            {
                // File was created
                var operation = new Operation(
                    $"Local file '{fileRelativePath}'.",
                    progress => DataAccessService.Instance.DownloadFile(remoteFile, fileFullPath, progress)
                );
                createOperations.Add(operation);
            }
        }
        foreach (string fileFullPath in Directory.GetFiles(fullPath))
        {
            var fileName = Path.GetFileName(fileFullPath);
            var fileRelativePath = Path.Join(relativePath, fileName);
            if (syncFolder.IgnoreListService.ShouldIgnore(fileRelativePath))
            {
                ConsoleHelpers.Info($"Skipping local file '{fileRelativePath}'.");
                continue;
            }
            if (remoteItemNames.Contains(fileName)) continue;

            // File was deleted
            var operation = new Operation(
                $"Local file '{fileRelativePath}'.",
                () => File.Delete(fileFullPath)
            );
            deleteOperations.Add(operation);
        }
        if (depth >= syncFolder.Depth) return;

        // Handle folders
        foreach (string folderFullPath in Directory.GetDirectories(fullPath))
        {
            var folderName = Path.GetFileName(folderFullPath);
            var folderRelativePath = Path.Join(relativePath, folderName);
            if (syncFolder.IgnoreListService.ShouldIgnore(folderRelativePath))
            {
                ConsoleHelpers.Info($"Skipping local folder '{folderRelativePath}'.");
                continue;
            }
            if (remoteItemNames.Contains(folderName)) continue;

            // The folder was deleted
            var operation = new Operation(
                $"Local folder '{folderRelativePath}'.",
                () => Directory.Delete(folderFullPath, true)
            );
            deleteOperations.Add(operation);
        }
        foreach (var remoteFolder in remoteFolders)
        {
            var folderFullPath = Path.Join(fullPath, remoteFolder.Name);
            var folderRelativePath = Path.Join(relativePath, remoteFolder.Name);
            if (syncFolder.IgnoreListService.ShouldIgnore(folderRelativePath))
            {
                ConsoleHelpers.Info($"Skipping remote folder '{folderRelativePath}' ({remoteFolder.Id}).");
                continue;
            }

            // Tail end recursion
            Directory.CreateDirectory(folderFullPath);
            history.Push(remoteFolder);
            AggregateOperations(
                syncFolder,
                createOperations,
                updateOperations,
                deleteOperations,
                history,
                remoteFolder.Id,
                depth + 1
            );
        }
    }
}