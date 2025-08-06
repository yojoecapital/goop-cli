using System;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using GoogleDrivePushCli.Models;

namespace GoogleDrivePushCli.Utilities;

public static class LinkFileHelper
{
    private static readonly string windowsExtension = ".url";
    private static readonly string osxExtension = ".webloc";
    private static readonly string linuxExtension = ".desktop";
    private static readonly string googleNativeMimeType = "application/vnd.google-apps.";

    private static string GetLinkFileTemplatePath()
    {
        var matchingFiles = Directory.GetFiles(Defaults.configurationPath, Defaults.linkTempalteFilePattern);
        if (matchingFiles.Length == 0) return null;
        return matchingFiles[0];
    }

    public static string GetLinkFileExtension()
    {
        var linkFileTemplatePath = GetLinkFileTemplatePath();
        if (linkFileTemplatePath != null) return Path.GetExtension(linkFileTemplatePath);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return windowsExtension;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return osxExtension;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return linuxExtension;
        }
        throw new PlatformNotSupportedException("Unsupported operating system");
    }

    public static bool IsGoogleDriveNativeFile(string mimeType)
    {
        return mimeType.StartsWith(googleNativeMimeType) && mimeType != RemoteFolder.MimeType;
    }

    public static void CreateLinkFile(string name, string url, string filePath)
    {
        var linkFileTemplatePath = GetLinkFileTemplatePath();
        if (linkFileTemplatePath == null)
        {
            CreateDefaultLinkFile(name, url, filePath);
            return;
        }

        FileManagementHelpers.CopyFileWithPermissions(linkFileTemplatePath, filePath);
        string content = File.ReadAllText(filePath).Replace("%NAME%", name).Replace("%URL%", url);
        File.WriteAllText(filePath, content);
    }

    private static void CreateDefaultLinkFile(string name, string url, string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string content = $"[InternetShortcut]\r\nURL={url}\r\n";
            File.WriteAllText(filePath, content);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string content = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
  <dict>
    <key>URL</key>
    <string>{url}</string>
  </dict>
</plist>";
            File.WriteAllText(filePath, content);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string content = $@"[Desktop Entry]
Encoding=UTF-8
Type=Link
Name={name}
URL={url}
";
            File.WriteAllText(filePath, content);
            try
            {
                var chmod = System.Diagnostics.Process.Start("chmod", $"+x \"{filePath}\"");
                chmod.WaitForExit();
            }
            catch
            {
                ConsoleHelpers.Info($"Failed to make the file '{filePath}' executable");
            }
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported operating system");
        }
    }
}