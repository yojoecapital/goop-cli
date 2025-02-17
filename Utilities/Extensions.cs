using System;

namespace GoogleDrivePushCli.Utilities
{
    public static class Extensions
    {
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int index = text.IndexOf(search);
            if (index < 0)
            {
                return text;
            }
            return string.Concat(text.AsSpan(0, index), replace, text.AsSpan(index + search.Length));
        }

        public static string ToFileSize(this long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            double size = bytes;
            string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];
            int unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            return $"{size:0.##} {units[unitIndex]}";
        }
    }
}