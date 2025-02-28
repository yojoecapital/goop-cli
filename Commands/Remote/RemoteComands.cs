using System.CommandLine;

namespace GoogleDrivePushCli.Commands.Remote;

public class RemoteCommands : Command
{
    public RemoteCommands() : base("remote", "Subcommands for managing remote items.")
    {
        AddCommand(new InformationCommand());
        AddCommand(new ListCommand());
        AddCommand(new MoveCommand());
        AddCommand(new TrashCommand());
    }
}