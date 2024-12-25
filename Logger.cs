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

        private static int lastBarLength = 0;
        public static void ProgressBar(double fraction)
        {
            if (verbose) return;
            fraction = Math.Max(0, Math.Min(1, fraction));
            int terminalWidth = Console.WindowWidth;
            const int extraSpace = 10; // 2 for "[]" + 8 for " 100% "
            int barWidth = Math.Max(0, terminalWidth - extraSpace);
            int filledWidth = (int)(fraction * barWidth);
            string bar = "[" + new string('=', filledWidth) + new string(' ', barWidth - filledWidth) + "]";

            // Erase the previous bar by overwriting
            if (lastBarLength > 0) Console.Write("\r" + new string(' ', lastBarLength) + "\r");

            // Print the bar and fraction
            string output = $"{bar} {fraction:P0}";
            Console.Write(output);

            // Update the last bar length
            lastBarLength = output.Length;
        }
    }
}
