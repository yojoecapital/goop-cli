using System;
using System.Linq;

namespace GoogleDrivePushCli
{
    public static class Logger
    {
        public static bool Verbose { get; set; }
        public static void Info(object message, int depth = 0)
        {
            if (!Verbose) return;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"[INFO] {Repeat(depth)}{message}");
            Console.ResetColor();
        }

        public static void Error(object message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }

        public static void ToDo(object message, int depth = 0) => Console.WriteLine($"[TODO] {Repeat(depth)}{message}");
        public static void Message(object message, int depth = 0) => Console.WriteLine((Repeat(depth) + message).PadRight(16));

        public static void Percent(int current, int total)
        {
            var percent = Math.Max(0, Math.Min(0.9999f, (float)current / total)) * 100;
            if (float.IsNaN(percent)) percent = 0;
            Console.Write($"[%...] {percent:F2}    \r");
        }

        private static string Repeat(int count) => string.Concat(Enumerable.Repeat("+ ", count));
    }
}
