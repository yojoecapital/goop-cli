using System;
using System.CommandLine;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class TrashCommand : Command
{
    public TrashCommand() : base("trash", "Trash a remote item.")
    {
        AddAlias("rm");
        var listOption = new Option<bool>("--list", "List the items in the trash.");
        var emptyOption = new Option<bool>("--empty", "Empty the trash.");
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
        var service = DataAccessService.Instance;
        var shouldTrash = isInteractive || !string.IsNullOrEmpty(path) || (!shouldList && !shouldEmpty);
        if (shouldTrash)
        {
            var remoteItem = RemotePathResolver.ResolveItem(path, isInteractive, "Select a remote item to trash.", "Trash this folder");
            if (remoteItem == null) return;
            if (remoteItem.Id == service.RootId) throw new Exception("Cannot trash the root folder");
            if (!skipConfirmation && !AnsiConsole.Confirm($"Move '{remoteItem.Name.EscapeMarkup()}' to the trash?", false)) return;
            service.TrashRemoteItem(remoteItem.Id);
            Console.WriteLine($"Trashed '{remoteItem.Name}' ({remoteItem.Id}).");
        }

        if (shouldList)
        {
            service.GetRemoteItemsInTrash(out var remoteFiles, out var remoteFolders);
            if (remoteFolders.Count == 0 && remoteFiles.Count == 0) Console.WriteLine("The trash is empty.");
            foreach (var remoteFolder in remoteFolders) Console.WriteLine(remoteFolder);
            foreach (var remoteFile in remoteFiles) Console.WriteLine(remoteFile);
        }

        if (shouldEmpty)
        {
            if (!skipConfirmation && !AnsiConsole.Confirm("Permanently delete everything in the trash?", false)) return;
            service.EmptyTrash();
            Console.WriteLine("Emptied the trash.");
        }
    }
}
