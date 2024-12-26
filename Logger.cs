using System;
using System.Linq;

namespace GoogleDrivePushCli
{
    internal static class Logger
    {
        // The verbose flag will control whether INFO level messages are logged
        public static bool verbose;

        public static void Info(string message, int depth = 0)
        {
            if (!verbose) return;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"[INFO] {Repeat(depth)}{message}");
            Console.ResetColor();
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }

        public static void ToDo(string message, int depth = 0) => Console.WriteLine($"[TODO] {Repeat(depth)}{message}");
        public static void Message(string message, int depth = 0) => Console.WriteLine((Repeat(depth) + message).PadRight(16));

        public static void Percent(int current, int total)
        {
            var fraction = Math.Max(0, Math.Min(1, (float)current / total));
            Console.Write($"[%...] {fraction * 100:F2}    \r");
        }

        private static string Repeat(int count) => string.Concat(Enumerable.Repeat("+ ", count));
    }
}
