using System;

internal partial class Program
{
    private static void WriteInfo(string message)
    {
        if (!verbose) return;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"[INFO] {message}");
        Console.ResetColor();
    }

    // Method to write error messages
    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }

    private static void WriteToDo(string message)
    {
        Console.Error.WriteLine($"[TODO] {message}");
    }
}