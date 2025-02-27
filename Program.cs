using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using GoogleDrivePushCli.Commands.Remote;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli
{
    public static partial class Program
    {
        public static readonly string version = "2.2.0";
        private static readonly Option<bool> verboseOption = new(["--verbose", "-vb"], "Show [INFO] messages.");

        [STAThread]
        public static int Main(string[] args)
        {
            var rootCommand = new RootCommand($"The {Defaults.applicationName} is a simple tool for syncing files between a local directory and Google Drive.")
            {
                new InformationCommand(),
                new ListCommand(),
                new MoveCommand()
            };
            rootCommand.AddGlobalOption(verboseOption);

            // Run the application
            var cli = new CommandLineBuilder(rootCommand)
                .AddMiddleware(InitializeHandler)
                .UseHelp()
                .AddMiddleware(VersionHandler)
                .UseParseErrorReporting()
                .UseExceptionHandler(ExceptionHandler)
                .Build();

            return cli.Invoke(args);
        }

        private static string GetEnvironmentCurrentDirectory() => Environment.CurrentDirectory;

        private static Task InitializeHandler(InvocationContext context, Func<InvocationContext, Task> next)
        {
            ConsoleHelpers.Verbose = context.ParseResult.GetValueForOption(verboseOption);
            Directory.CreateDirectory(Defaults.configurationPath);
            return next(context);
        }

        private static Task VersionHandler(InvocationContext context, Func<InvocationContext, Task> next)
        {
            var tokens = context.ParseResult.Tokens;
            if (tokens.Count != 1) return next(context);
            var firstToken = tokens[0].ToString();
            if (firstToken == "-v" || firstToken == "--version" || firstToken == "version")
            {
                Console.WriteLine(version);
                return Task.CompletedTask;
            }
            return next(context);
        }

        private static void ExceptionHandler(Exception ex, InvocationContext context)
        {
            ConsoleHelpers.Error(ex.Message);
#if DEBUG
            Console.WriteLine(ex.StackTrace);
#endif
            context.ExitCode = 1;
        }
    }
}