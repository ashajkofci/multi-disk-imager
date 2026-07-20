using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MultiDiskImager.Services;

internal static class NotificationService
{
    public static void Notify()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                MessageBeep(0x00000040);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo("/usr/bin/osascript")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ArgumentList = { "-e", "beep 1" }
                })?.Dispose();
            }
            else if (OperatingSystem.IsLinux() && File.Exists("/usr/bin/canberra-gtk-play"))
            {
                Process.Start(new ProcessStartInfo("/usr/bin/canberra-gtk-play")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ArgumentList = { "--id=complete" }
                })?.Dispose();
            }
        }
        catch (Exception)
        {
            // A notification is optional and must never turn a successful image into a failure.
        }
    }

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MessageBeep(uint type);
}
