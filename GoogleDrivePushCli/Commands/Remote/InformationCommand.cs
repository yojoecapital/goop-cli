using System.CommandLine;
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
        RemoteItem remoteItem;
        if (isInteractive)
        {
            remoteItem = NavigationHelper.Navigate(path)?.Peek();
            if (remoteItem == null) return;
        }
        else remoteItem = DataAccessManager.Instance.GetRemoteItemsFromPath(path).Peek();
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        if (remoteItem is RemoteFile remoteFile)
        {
            grid.AddRow(["[bold]ID[/]", $": {remoteFile.Id}"]);
            grid.AddRow(["[bold]Name[/]", $": {remoteFile.Name.EscapeMarkup()}"]);
            grid.AddRow(["[bold]MIME type[/]", $": {remoteFile.MimeType}"]);
            grid.AddRow(["[bold]Modified time[/]", $": {remoteFile.ModifiedTime}"]);
            grid.AddRow(["[bold]Size[/]", $": {remoteFile.Size.ToFileSize()}"]);
        }
        else
        {
            grid.AddRow(["[bold]ID[/]", $": {remoteItem.Id}"]);
            grid.AddRow(["[bold]Name[/]", $": {remoteItem.Name.EscapeMarkup()}"]);
            grid.AddRow(["[bold]MIME type[/]", $": {RemoteFolder.MimeType}"]);
        }
        AnsiConsole.Write(grid);
    }
}
