using System.Collections;
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
        var remotePath = $"{string.Join('/', remoteItems.Select(remoteItem => remoteItem.Name).Reverse())}";
        var grid = new Grid();
        grid.AddColumns(2);
        if (remoteItem is RemoteFile remoteFile)
        {
            grid.AddRow(["[bold]ID[/]", $": {remoteFile.Id}"]);
            grid.AddRow(["[bold]Name[/]", $": {remoteFile.Name.EscapeMarkup()}"]);
            grid.AddRow(["[bold]MIME type[/]", $": {remoteFile.MimeType}"]);
            grid.AddRow(["[bold]Modified time[/]", $": {remoteFile.ModifiedTime}"]);
            grid.AddRow(["[bold]Size[/]", $": {remoteFile.Size.ToFileSize()}"]);
            grid.AddRow(["[bold]Path[/]", $": {remotePath}"]);
        }
        else
        {
            grid.AddRow(["[bold]ID[/]", $": {remoteItem.Id}"]);
            grid.AddRow(["[bold]Name[/]", $": {remoteItem.Name.EscapeMarkup()}"]);
            grid.AddRow(["[bold]MIME type[/]", $": {RemoteFolder.MimeType}"]);
            grid.AddRow(["[bold]Path[/]", $": {remotePath}"]);
        }
        AnsiConsole.Write(grid);
    }
}
