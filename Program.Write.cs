using System;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        public static void WriteInfo(string message)
        {
            if (!verbose) return;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"[INFO] {message}");
            Console.ResetColor();
        }

        // Method to write error messages
        public static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }

        public static void WriteToDo(string message)
        {
            Console.Error.WriteLine($"[TODO] {message}");
        }
    }
}