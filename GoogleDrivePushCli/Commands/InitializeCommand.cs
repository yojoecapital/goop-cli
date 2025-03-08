using System;
using System.CommandLine;
using System.IO;
using GoogleDrivePushCli.Json.SyncFolder;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Commands;

public class InitializeCommand : Command
{
    public InitializeCommand() : base("initialize", $"Initialize a new sync folder by creating a '{Defaults.syncFolderFileName}' file.")
    {
        AddAlias("init");
        var remoteFolderPathArgument = new Argument<string>("path", "The path of the remote folder to sync.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        AddArgument(remoteFolderPathArgument);
        AddOption(DefaultParameters.depthOption);
        AddOption(DefaultParameters.interactiveOption);
        AddOption(DefaultParameters.workingDirectoryOption);
        this.SetHandler(
            Handle,
            remoteFolderPathArgument,
            DefaultParameters.depthOption,
            DefaultParameters.interactiveOption,
            DefaultParameters.workingDirectoryOption
        );
    }

    private static void Handle(string remoteFolderPath, int depth, bool isInteractive, string workingDirectory)
    {
        var directory = SyncFolder.FindRoot(workingDirectory);
        if (directory != null)
        {
            throw new Exception($"A '{Defaults.syncFolderFileName}' file already exists in '{directory}'");
        }
        Directory.CreateDirectory(workingDirectory);
        if (string.IsNullOrEmpty(remoteFolderPath))
        {
            isInteractive = true;
            remoteFolderPath = "/";
        }
        RemoteItem remoteFolder;
        if (isInteractive)
        {
            remoteFolder = NavigationHelper.Navigate(
                remoteFolderPath,
                new()
                {
                    prompt = $"Select a remote file to download.",
                    onlyDisplayFolders = true
                }
            )?.Peek();
            if (remoteFolder == null) return;
        }
        else remoteFolder = DataAccessService.Instance.GetRemoteItemsFromPath(remoteFolderPath).Peek();
        if (remoteFolder is not RemoteFolder)
        {
            throw new Exception($"Path argument must be a remote folder. Remote item '{remoteFolder.Name}' ({remoteFolder.Id}) is not a folder");
        }
        var syncFolder = new SyncFolder()
        {
            FolderId = remoteFolder.Id,
            Depth = depth
        };
        syncFolder.Save(workingDirectory);
        Console.WriteLine($"Initialized sync folder at '{workingDirectory}'. Ready to pull.");
    }
}