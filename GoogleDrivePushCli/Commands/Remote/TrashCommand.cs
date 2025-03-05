using System;
using System.CommandLine;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class TrashCommand : Command
{
    public TrashCommand() : base("trash", "Trash an item.")
    {
        var listOption = new Option<bool>(
            "--list",
            "List the items in the trash."
        );
        var emptyOption = new Option<bool>(
            "--empty",
            "Empty the trash."
        );
        AddArgument(DefaultParameters.pathArgument);
        AddOption(DefaultParameters.interactiveOption);
        AddOption(listOption);
        AddOption(emptyOption);
        AddOption(DefaultParameters.yesOption);
        this.SetHandler(
            Handle,
            DefaultParameters.pathArgument,
            DefaultParameters.interactiveOption,
            listOption,
            emptyOption,
            DefaultParameters.yesOption
        );
    }

    private static void Handle(string path, bool isInteractive, bool shouldList, bool shouldEmpty, bool skipConfirmation)
    {
        // handle the path argument
        RemoteItem remoteItem = null;
        if (isInteractive || string.IsNullOrEmpty(path) && !shouldList && !shouldEmpty)
        {
            if (string.IsNullOrEmpty(path)) path = "/";
            remoteItem = NavigationHelper.Navigate(path, new()
            {
                selectThisText = "Trash this folder"
            }).Peek();
        }
        else if (!string.IsNullOrEmpty(path))
        {
            remoteItem = DataAccessManager.Instance.GetRemoteItemsFromPath(path).Peek();
        }
        if (remoteItem != null)
        {
            if (remoteItem.Id == DataAccessManager.Instance.RootId)
            {
                throw new Exception("Cannot trash root folder");
            }
            DataAccessManager.Instance.TrashRemoteItem(remoteItem.Id);
        }

        // handle the list option
        if (shouldList)
        {
            DataAccessManager.Instance.GetRemoteItemsInTrash(out var remoteFiles, out var remoteFolders);
            if (shouldEmpty) Console.ForegroundColor = ConsoleColor.Red;
            foreach (var remoteFile in remoteFiles) Console.WriteLine(remoteFile);
            foreach (var remoteFolder in remoteFolders) Console.WriteLine(remoteFolder);
            Console.ResetColor();
        }

        // handle the empty option
        if (shouldEmpty)
        {
            if (skipConfirmation || AnsiConsole.Confirm("Are you sure you want to empty the trash?", false))
            {
                DataAccessManager.Instance.EmptyTrash();
            }
        }
    }
}