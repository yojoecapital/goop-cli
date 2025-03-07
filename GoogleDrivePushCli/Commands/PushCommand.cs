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

public class PushCommand : Command
{
    public PushCommand() : base("push", "Pushes local changes to Google Drive.")
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
        Dictionary<string, RemoteFile> remoteFilesMap = [];
        Dictionary<string, RemoteFolder> remoteFoldersMap = [];
        foreach (var remoteFile in remoteFiles) remoteFilesMap[remoteFile.Name] = remoteFile;
        foreach (var remoteFolder in remoteFolders) remoteFoldersMap[remoteFolder.Name] = remoteFolder;

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
            if (File.Exists(fileFullPath)) continue;

            // File was deleted
            var operation = new Operation(
                $"Remote file '{fileRelativePath}'.",
                progress => DataAccessService.Instance.TrashRemoteItem(remoteFile.Id)
            );
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
            if (remoteFilesMap.TryGetValue(fileName, out var remoteFile))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(fileFullPath);
                if (lastWriteTime <= remoteFile.ModifiedTime) continue;

                // File was edited
                var operation = new Operation(
                    $"Remote file '{fileRelativePath}'.",
                    progress => DataAccessService.Instance.UpdateRemoteFile(remoteFile.Id, fileFullPath, progress)
                );
                updateOperations.Add(operation);
            }
            else
            {
                // File was created
                var operation = new Operation(
                    $"Remote file '{fileRelativePath}'.",
                    progress => DataAccessService.Instance.CreateRemoteFile(remoteFile.Id, fileFullPath, progress)
                );
                createOperations.Add(operation);
            }
        }
        if (depth >= syncFolder.Depth) return;

        // Handle folders
        foreach (var remoteFolder in remoteFolders)
        {
            var folderFullPath = Path.Join(fullPath, remoteFolder.Name);
            var folderRelativePath = Path.Join(relativePath, remoteFolder.Name);
            if (syncFolder.IgnoreListService.ShouldIgnore(folderRelativePath))
            {
                ConsoleHelpers.Info($"Skipping remote folder '{folderRelativePath}' ({remoteFolder.Id}).");
                continue;
            }
            if (Directory.Exists(folderFullPath)) continue;

            // The folder was deleted
            var operation = new Operation(
                $"Remote folder '{folderRelativePath}'.",
                () => DataAccessService.Instance.TrashRemoteItem(remoteFolder.Id)
            );
            deleteOperations.Add(operation);
        }
        foreach (string folderFullPath in Directory.GetDirectories(fullPath))
        {
            var folderName = Path.GetFileName(folderFullPath);
            var folderRelativePath = Path.Join(relativePath, folderName);
            if (syncFolder.IgnoreListService.ShouldIgnore(folderRelativePath))
            {
                ConsoleHelpers.Info($"Skipping local folder '{folderRelativePath}'.");
                continue;
            }

            // Ensure the folder exists
            if (!remoteFoldersMap.TryGetValue(folderName, out RemoteFolder remoteFolder))
            {
                remoteFolder = DataAccessService.Instance.CreateRemoteFolder(remoteFolderId, folderName);
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