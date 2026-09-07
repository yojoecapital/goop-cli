using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class InformationCommand : Command
{
    public InformationCommand() : base("information", "Get information for a remote item.")
    {
        AddAlias("info");
        AddArgument(DefaultParameters.pathArgument);
        AddOption(DefaultParameters.interactiveOption);
        this.SetHandler(
            Handle,
            DefaultParameters.pathArgument,
            DefaultParameters.interactiveOption
        );
    }

    private static void Handle(string path, bool isInteractive)
    {
        if (string.IsNullOrEmpty(path))
        {
            isInteractive = true;
            path = "/";
        }
        Stack<RemoteItem> remoteItems;
        if (isInteractive)
        {
            remoteItems = NavigationHelper.Navigate(path);
            if (remoteItems == null) return;
        }
        else remoteItems = DataAccessService.Instance.GetRemoteItemsFromPath(path);
        var remoteItem = remoteItems.Peek();
        var remotePath = string.Join('/', remoteItems.Select(item => item.Name).Reverse());
        var grid = new Grid();
        grid.AddColumns(2);
        AddRow(grid, "ID", remoteItem.Id);
        AddRow(grid, "Name", remoteItem.EffectiveRemoteName);
        if (remoteItem is RemoteFile remoteFile)
        {
            AddRow(grid, "MIME type", remoteFile.MimeType);
            AddRow(grid, "Modified time", remoteFile.ModifiedTime.ToUtcDateTime().ToLocalTime().ToString());
            if (remoteFile.IsLink)
            {
                AddRow(grid, "Local name", remoteFile.Name);
                AddRow(grid, "URL", remoteFile.WebViewLink);
            }
            else AddRow(grid, "Size", remoteFile.Size.ToFileSize());
        }
        else AddRow(grid, "MIME type", RemoteFolder.MimeType);
        AddRow(grid, "Path", remotePath);
        AnsiConsole.Write(grid);
    }

    private static void AddRow(Grid grid, string label, string value) =>
        grid.AddRow([$"[bold]{label}[/]", $": {(value ?? string.Empty).EscapeMarkup()}"]);
}
