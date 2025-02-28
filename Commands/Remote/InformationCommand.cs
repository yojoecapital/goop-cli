using System.CommandLine;
using GoogleDrivePushCli.Data.Models;
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
        RemoteItem item;
        if (isInteractive)
        {
            item = NavigationHelper.Navigate(path)?.Peek();
            if (item == null) return;
        }
        else item = DriveServiceWrapper.Instance.GetItemsFromPath(path).Peek();
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow(["[bold]ID[/]", $": {item.Id}"]);
        grid.AddRow(["[bold]Name[/]", $": {item.Name.EscapeMarkup()}"]);
        grid.AddRow(["[bold]MIME type[/]", $": {item.MimeType}"]);
        grid.AddRow(["[bold]Modified time[/]", $": {item.ModifiedTime}"]);
        if (item.Size.HasValue) grid.AddRow(["[bold]Size[/]", $": {item.Size.Value.ToFileSize()}"]);
        AnsiConsole.Write(grid);
    }
}
