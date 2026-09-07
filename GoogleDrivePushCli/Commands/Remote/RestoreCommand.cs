using System;
using System.CommandLine;
using System.Linq;
using GoogleDrivePushCli.Services;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class RestoreCommand : Command
{
    public RestoreCommand() : base("restore", "Restore an item from the trash.")
    {
        AddAlias("untrash");
        var nameArgument = new Argument<string>("name", "The name of the trashed item to restore.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        AddArgument(nameArgument);
        AddOption(DefaultParameters.yesOption);
        this.SetHandler(Handle, nameArgument, DefaultParameters.yesOption);
    }

    private static void Handle(string name, bool skipConfirmation)
    {
        var service = DataAccessService.Instance;
        service.GetRemoteItemsInTrash(out var remoteFiles, out var remoteFolders);
        var trashedItems = remoteFolders
            .Cast<Models.RemoteItem>()
            .Concat(remoteFiles)
            .ToList();
        if (trashedItems.Count == 0)
        {
            Console.WriteLine("The trash is empty.");
            return;
        }
        var matches = string.IsNullOrEmpty(name)
            ? trashedItems
            : [.. trashedItems.Where(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || item.EffectiveRemoteName.Equals(name, StringComparison.OrdinalIgnoreCase))];
        if (matches.Count == 0) throw new Exception($"No trashed item named '{name}' was found");
        var selected = matches.Count == 1 && !string.IsNullOrEmpty(name)
            ? matches[0]
            : AnsiConsole.Prompt(
                new SelectionPrompt<Models.RemoteItem>()
                    .Title("Select an item to restore from the trash:")
                    .PageSize(10)
                    .WrapAround()
                    .UseConverter(item => item.ToString().EscapeMarkup())
                    .AddChoices(matches)
            );
        if (!skipConfirmation && !AnsiConsole.Confirm($"Restore '{selected.Name}' from the trash?", true)) return;
        var restored = service.RestoreRemoteItemFromTrash(selected.Id);
        Console.WriteLine($"Restored '{restored.Name}' ({restored.Id}) from the trash.");
    }
}
