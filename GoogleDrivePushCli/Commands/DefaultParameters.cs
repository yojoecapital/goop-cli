using System.CommandLine;

namespace GoogleDrivePushCli.Commands
{
    public static class DefaultParameters
    {
        public static readonly Option<bool> interactiveOption = new(
            ["--interactive", "-i"],
            "Open to the path and use an interactive prompt."
        );
        public static readonly Option<bool> yesOption = new(
            ["--yes", "-y"],
            "Skip the confirmation prompt."
        );

        public static readonly Argument<string> pathArgument = new(
            "path",
            "The path of the remote item."
        )
        {
            Arity = ArgumentArity.ZeroOrOne
        };
    }
}