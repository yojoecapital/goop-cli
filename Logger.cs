using System;

namespace GoogleDrivePushCli
{
    internal static class Logger
    {
        // The verbose flag will control whether INFO level messages are logged
        public static bool verbose;

        public static void Info(string message)
        {
            if (!verbose) return;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"[INFO] {message}");
            Console.ResetColor();
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }

        public static void ToDo(string message) => Console.WriteLine($"[TODO] {message}");
        public static void Message(string message) => Console.WriteLine(message);
    }
}
