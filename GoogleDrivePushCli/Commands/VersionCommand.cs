using System;
using System.CommandLine;

namespace GoogleDrivePushCli.Commands;

public class VersionCommand : Command
{
    public VersionCommand() : base("version", "Show the version of the application.")
    {
        this.SetHandler(() => Console.WriteLine(Program.version));
    }
}
