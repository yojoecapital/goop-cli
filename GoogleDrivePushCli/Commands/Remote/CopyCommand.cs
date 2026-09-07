using System;
using System.CommandLine;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class CopyCommand : Command
{
    public CopyCommand() : base("copy", "Copy a remote item into a remote folder.")
    {
        AddAlias("cp");
        var remoteFolderPathOption = new Option<string>("--into", "The path of the remote folder to copy the item into.");
        var nameOption = new Option<string>(["--name", "-n"], "The name to give the copy.");
        AddArgument(DefaultParameters.pathArgument);
        AddOption(remoteFolderPathOption);
        AddOption(nameOption);
        AddOption(DefaultParameters.interactiveOption);
        AddOption(DefaultParameters.depthOption);
        this.SetHandler(
            Handle,
            DefaultParameters.pathArgument,
            remoteFolderPathOption,
            nameOption,
            DefaultParameters.interactiveOption,
            DefaultParameters.depthOption
        );
    }

    private static void Handle(string path, string remoteFolderPath, string name, bool isInteractive, int depth)
    {
        var remoteItem = RemotePathResolver.ResolveItem(path, isInteractive, "Select a remote item to copy.", "Copy this folder");
        if (remoteItem == null) return;
        if (remoteItem.Id == DataAccessService.Instance.RootId) throw new Exception("Cannot copy the root folder");
        var remoteFolder = RemotePathResolver.ResolveFolder(
            remoteFolderPath,
            isInteractive,
            $"Select a remote folder to copy '{remoteItem.Name}' into:",
            "Copy here"
        );
        if (remoteFolder == null) return;
        RemoteItem copy = null;
        AnsiConsole.Progress().Start(context =>
        {
            var task = context.AddTask($"Copying '{remoteItem.Name}'", maxValue: 1);
            OperationHelpers.Run(
                progress => copy = DataAccessService.Instance.CopyRemoteItem(remoteItem, remoteFolder.Id, name, depth, progress),
                task
            );
        });
        Console.WriteLine($"Copied '{remoteItem.Name}' into '{remoteFolder.Name}' as '{copy.Name}' ({copy.Id}).");
    }
}
