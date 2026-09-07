using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;

namespace GoogleDrivePushCli.Commands;

public class CompletionsCommand : Command
{
    private static readonly string[] supportedShells = ["bash", "zsh", "fish"];

    public CompletionsCommand() : base("completions", "Generate a shell completion script.")
    {
        var shellArgument = new Argument<string>("shell", $"The shell to generate completions for ({string.Join(", ", supportedShells)}).");
        shellArgument.FromAmong(supportedShells);
        AddArgument(shellArgument);
        this.SetHandler(Handle, shellArgument);
    }

    private static void Handle(string shell)
    {
        var rootCommand = Program.CreateRootCommand();
        Console.WriteLine(shell switch
        {
            "bash" => GenerateBash(rootCommand),
            "zsh" => GenerateZsh(rootCommand),
            "fish" => GenerateFish(rootCommand),
            _ => throw new ArgumentException($"Unsupported shell '{shell}'")
        });
    }

    private static string ExecutableName => "goop";

    private static IEnumerable<Command> VisibleSubcommands(Command command) =>
        command.Children.OfType<Command>().Where(child => !child.IsHidden);

    private static IEnumerable<string> OptionNames(Command command) =>
        command.Options.Where(option => !option.IsHidden).SelectMany(option => option.Aliases).Distinct();

    private static string CommandPathKey(IEnumerable<string> path) => string.Join('_', path);

    private static void Walk(Command command, List<string> path, Action<List<string>, Command> visit)
    {
        visit(path, command);
        foreach (var subcommand in VisibleSubcommands(command))
        {
            path.Add(subcommand.Name);
            Walk(subcommand, path, visit);
            path.RemoveAt(path.Count - 1);
        }
    }

    private static string GenerateBash(Command rootCommand)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"_{ExecutableName}_completions()");
        builder.AppendLine("{");
        builder.AppendLine("    local cur prev words cword");
        builder.AppendLine("    COMPREPLY=()");
        builder.AppendLine("    cur=\"${COMP_WORDS[COMP_CWORD]}\"");
        builder.AppendLine("    local key=\"\"");
        builder.AppendLine("    local i");
        builder.AppendLine("    for (( i=1; i < COMP_CWORD; i++ )); do");
        builder.AppendLine("        case \"${COMP_WORDS[i]}\" in");
        builder.AppendLine("            -*) continue ;;");
        builder.AppendLine("        esac");
        builder.AppendLine("        if [[ -z \"$key\" ]]; then key=\"${COMP_WORDS[i]}\"; else key=\"${key}_${COMP_WORDS[i]}\"; fi");
        builder.AppendLine("    done");
        builder.AppendLine("    local suggestions=\"\"");
        builder.AppendLine("    case \"$key\" in");

        var cases = new List<string>();
        Walk(rootCommand, [], (path, command) =>
        {
            var words = VisibleSubcommands(command).SelectMany(subcommand => subcommand.Aliases).Concat(OptionNames(command)).Distinct();
            cases.Add($"        {(path.Count == 0 ? "\"\"" : CommandPathKey(path))}) suggestions=\"{string.Join(' ', words)}\" ;;");
        });
        foreach (var line in cases) builder.AppendLine(line);

        builder.AppendLine("    esac");
        builder.AppendLine("    COMPREPLY=( $(compgen -W \"$suggestions\" -- \"$cur\") )");
        builder.AppendLine("    return 0");
        builder.AppendLine("}");
        builder.AppendLine($"complete -F _{ExecutableName}_completions {ExecutableName}");
        return builder.ToString();
    }

    private static string GenerateZsh(Command rootCommand)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"#compdef {ExecutableName}");
        builder.AppendLine($"_{ExecutableName}()");
        builder.AppendLine("{");
        builder.AppendLine("    local -a words_list");
        builder.AppendLine("    local key=\"\"");
        builder.AppendLine("    local i");
        builder.AppendLine("    for (( i = 2; i < CURRENT; i++ )); do");
        builder.AppendLine("        case \"${words[i]}\" in");
        builder.AppendLine("            -*) continue ;;");
        builder.AppendLine("        esac");
        builder.AppendLine("        if [[ -z \"$key\" ]]; then key=\"${words[i]}\"; else key=\"${key}_${words[i]}\"; fi");
        builder.AppendLine("    done");
        builder.AppendLine("    case \"$key\" in");

        Walk(rootCommand, [], (path, command) =>
        {
            var entries = VisibleSubcommands(command)
                .Select(subcommand => $"'{subcommand.Name}:{Escape(subcommand.Description)}'")
                .Concat(command.Options.Where(option => !option.IsHidden).Select(option => $"'{option.Aliases.First()}:{Escape(option.Description)}'"));
            builder.AppendLine($"        {(path.Count == 0 ? "\"\"" : CommandPathKey(path))}) words_list=({string.Join(' ', entries)}) ;;");
        });

        builder.AppendLine("    esac");
        builder.AppendLine("    _describe 'goop' words_list");
        builder.AppendLine("}");
        builder.AppendLine($"compdef _{ExecutableName} {ExecutableName}");
        return builder.ToString();
    }

    private static string GenerateFish(Command rootCommand)
    {
        var builder = new StringBuilder();
        var seenConditions = new HashSet<string>();
        Walk(rootCommand, [], (path, command) =>
        {
            var condition = path.Count == 0
                ? $"__fish_use_subcommand"
                : $"__fish_seen_subcommand_from {string.Join(' ', path)}";
            foreach (var subcommand in VisibleSubcommands(command))
            {
                builder.AppendLine($"complete -c {ExecutableName} -n '{condition}' -a '{subcommand.Name}' -d '{Escape(subcommand.Description)}'");
            }
            foreach (var option in command.Options.Where(option => !option.IsHidden))
            {
                foreach (var alias in option.Aliases.Distinct())
                {
                    var flag = alias.StartsWith("--", StringComparison.Ordinal) ? $"-l {alias[2..]}" : $"-s {alias.TrimStart('-')}";
                    var key = $"{condition}|{alias}";
                    if (!seenConditions.Add(key)) continue;
                    builder.AppendLine($"complete -c {ExecutableName} -n '{condition}' {flag} -d '{Escape(option.Description)}'");
                }
            }
        });
        return builder.ToString();
    }

    private static string Escape(string description) =>
        (description ?? string.Empty).Replace("'", "").Replace("\n", " ").Replace("\r", " ").Trim();
}
