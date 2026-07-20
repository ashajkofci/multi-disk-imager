using System.Diagnostics;
using System.Runtime.InteropServices;
using MultiDiskImager.Core;

namespace MultiDiskImager.Infrastructure;

internal sealed class PlatformPowerInhibitor : IPowerInhibitor
{
    private readonly Process? _caffeinate;

    private PlatformPowerInhibitor(Process? caffeinate) => _caffeinate = caffeinate;

    public static ValueTask<IPowerInhibitor> AcquireAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            _ = SetThreadExecutionState(0x80000000 | 0x00000001 | 0x00000040);
            return ValueTask.FromResult<IPowerInhibitor>(new PlatformPowerInhibitor(null));
        }

        if (OperatingSystem.IsMacOS())
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/caffeinate",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            return ValueTask.FromResult<IPowerInhibitor>(new PlatformPowerInhibitor(process));
        }

        if (OperatingSystem.IsLinux() && File.Exists("/usr/bin/systemd-inhibit"))
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/bin/systemd-inhibit",
                UseShellExecute = false,
                CreateNoWindow = true,
                ArgumentList =
                {
                    "--what=sleep:idle",
                    "--mode=block",
                    "--why=Raw disk imaging is in progress",
                    "/usr/bin/sleep",
                    "infinity"
                }
            });
            return ValueTask.FromResult<IPowerInhibitor>(new PlatformPowerInhibitor(process));
        }

        return ValueTask.FromResult<IPowerInhibitor>(new NullPowerInhibitor());
    }

    public ValueTask DisposeAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            _ = SetThreadExecutionState(0x80000000);
        }

        if (_caffeinate is { HasExited: false })
        {
            _caffeinate.Kill(entireProcessTree: true);
            _caffeinate.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    [DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint executionState);
}
