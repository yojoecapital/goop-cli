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
        int operationCount = createOperations.Count + updateOperations.Count + deleteOperations.Count;
        if (operationCount == 0)
        {
            Console.WriteLine("Up to date.");
            return;
        }
        foreach (var operation in createOperations) AnsiConsole.MarkupLineInterpolated($"[green][[CREATE]][/] {operation.Description}");
        foreach (var operation in updateOperations) AnsiConsole.MarkupLineInterpolated($"[yellow][[UPDATE]][/] {operation.Description}");
        foreach (var operation in deleteOperations) AnsiConsole.MarkupLineInterpolated($"[red][[DELETE]][/] {operation.Description}");
        if (!skipConfirmation && !AnsiConsole.Confirm($"Execute [bold]{operationCount}[/] operation(s)?", false))
        {
            return;
        }

        AnsiConsole.Progress().Start(context =>
        {
            var createTask = createOperations.Count > 0 ? context.AddTask("[green]Creating items[/]", maxValue: createOperations.Count) : null;
            var updateTask = updateOperations.Count > 0 ? context.AddTask("[yellow]Updating items[/]", maxValue: updateOperations.Count) : null;
            var deleteTask = deleteOperations.Count > 0 ? context.AddTask("[red]Deleting items[/]", maxValue: updateOperations.Count) : null;
            foreach (var operation in createOperations)
            {
                operation.Run(createTask);
            }
            foreach (var operation in updateOperations)
            {
                operation.Run(updateTask);
            }
            foreach (var operation in deleteOperations)
            {
                operation.Run(deleteTask);
            }
        });
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
        var remoteItems = remoteFiles
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
                // File was edited
                var lastWriteTime = File.GetLastWriteTimeUtc(fileFullPath);
                if (lastWriteTime >= remoteFile.ModifiedTime) continue;
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
            if (remoteItems.Contains(fileName)) continue;

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
            if (remoteItems.Contains(folderName)) continue;

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