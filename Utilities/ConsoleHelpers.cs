using System;
using Spectre.Console;

namespace GoogleDrivePushCli.Utilities;
public static class ConsoleHelpers
{
    public static bool Verbose { get; set; } = false;

    public static void Error(object value)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold red][[ERROR]][/] {value}");
    }

    public static void Info(object value)
    {
        if (!Verbose) return;
        AnsiConsole.MarkupLineInterpolated($"[blue][[INFO]][/] {value}");
    }

    public static void Clear(int row)
    {
        Console.SetCursorPosition(0, row);
        for (var i = row; i < Console.WindowHeight; i++)
        {
            Console.Write(new string(' ', Console.WindowWidth));
        }
        Console.SetCursorPosition(0, row);
    }
}
