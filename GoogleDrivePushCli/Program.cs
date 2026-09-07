using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using GoogleDrivePushCli.Commands;
using GoogleDrivePushCli.Commands.Remote;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli
{
    public static class Program
    {
        public static readonly string version = "3.2.0";
        private static readonly Option<bool> verboseOption = new(["--verbose"], "Show [INFO] messages.");

        public static RootCommand CreateRootCommand()
        {
            var rootCommand = new RootCommand($"The {Defaults.applicationName} is a simple tool for syncing files between a local directory and Google Drive.")
            {
                new RemoteCommands(),
                new ClearCache(),
                new InitializeCommand(),
                new StatusCommand(),
                new DifferencesCommand(),
                new PullCommand(),
                new PushCommand(),
                new ConfigurationCommand(),
                new CompletionsCommand(),
                new VersionCommand()
            };
            rootCommand.AddGlobalOption(verboseOption);
            return rootCommand;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            Defaults.EnsureConfigurationDirectory();
            var cli = new CommandLineBuilder(CreateRootCommand())
                .AddMiddleware(InitializeHandler)
                .UseHelp()
                .AddMiddleware(VersionHandler)
                .UseParseErrorReporting()
                .UseExceptionHandler(ExceptionHandler)
                .Build();
            try
            {
                return cli.Invoke(args);
            }
            finally
            {
                DataAccessService.Shutdown();
            }
        }

        private static Task InitializeHandler(InvocationContext context, Func<InvocationContext, Task> next)
        {
            ConsoleHelpers.Verbose = context.ParseResult.GetValueForOption(verboseOption);
            return next(context);
        }

        private static Task VersionHandler(InvocationContext context, Func<InvocationContext, Task> next)
        {
            var tokens = context.ParseResult.Tokens;
            if (tokens.Count != 1) return next(context);
            var firstToken = tokens[0].ToString();
            if (firstToken == "-v" || firstToken == "--version")
            {
                Console.WriteLine(version);
                return Task.CompletedTask;
            }
            return next(context);
        }

        private static void ExceptionHandler(Exception exception, InvocationContext context)
        {
            string message = exception.Message.EndsWith('.') ? exception.Message : $"{exception.Message}.";
            ConsoleHelpers.Error(message);
#if DEBUG
            Console.WriteLine(exception.StackTrace);
#endif
            ConsoleHelpers.RestoreCursor();
            context.ExitCode = 1;
        }
    }
}
