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
