using System.Diagnostics;
using System.Runtime.Versioning;
using MultiDiskImager.Core;
using MultiDiskImager.Infrastructure;
using MultiDiskImager.Platform;

namespace MultiDiskImager.Core.Tests;

[SupportedOSPlatform("macos")]
public sealed class MacPlatformTests
{
    [Fact]
    public async Task SelectionValidationDoesNotInspectEachSelectedDisk()
    {
        var calls = new List<string[]>();
        var catalog = new MacDeviceCatalog((_, arguments, _) =>
        {
            var values = arguments.ToArray();
            calls.Add(values);
            return Task.FromResult(values.SequenceEqual(["list", "-plist", "physical"])
                ? new ProcessResult(0, PhysicalDiskList(size: 64_000), string.Empty)
                : new ProcessResult(0, SystemDiskInfo(), string.Empty));
        });
        var selected = SelectedDisk(size: 64_000);

        var validated = await catalog.ValidateSelectionsAsync([selected]);

        Assert.Equal(selected.Id, Assert.Single(validated).Id);
        Assert.Equal(2, calls.Count);
        Assert.Contains(calls, call => call.SequenceEqual(["list", "-plist", "physical"]));
        Assert.Contains(calls, call => call.SequenceEqual(["info", "-plist", "/"]));
        Assert.DoesNotContain(calls, call => call.Contains("disk4", StringComparer.Ordinal));
    }

    [Fact]
    public async Task SelectionValidationRejectsAChangedDiskSize()
    {
        var catalog = CatalogWithDiskSize(128_000);

        var exception = await Assert.ThrowsAsync<IOException>(() =>
            catalog.ValidateSelectionsAsync([SelectedDisk(size: 64_000)]));

        Assert.Contains("changed after selection", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SelectionValidationRejectsTheSystemDisk()
    {
        var catalog = new MacDeviceCatalog((_, arguments, _) =>
        {
            var values = arguments.ToArray();
            return Task.FromResult(values[0] == "list"
                ? new ProcessResult(0, PhysicalDiskList(size: 64_000), string.Empty)
                : new ProcessResult(0, SystemDiskInfo("disk4"), string.Empty));
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            catalog.ValidateSelectionsAsync([SelectedDisk(size: 64_000)]));
    }

    [Fact]
    public async Task SelectionValidationTurnsAStuckDiskUtilCallIntoATimeoutError()
    {
        var catalog = new MacDeviceCatalog(
            async (_, _, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("Unreachable");
            },
            TimeSpan.FromMilliseconds(20));

        var exception = await Assert.ThrowsAsync<IOException>(() =>
            catalog.ValidateSelectionsAsync([SelectedDisk(size: 64_000)]));

        Assert.Contains("Disk validation timed out", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PollingWaitObservesACompletedChildProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(OperatingSystem.IsWindows() ? "/c" : "-c");
        startInfo.ArgumentList.Add("exit 0");
        using var process = new Process
        {
            StartInfo = startInfo
        };
        Assert.True(process.Start());

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await ProcessRunner.WaitForExitByPollingAsync(process, timeout.Token);

        Assert.True(process.HasExited);
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public async Task RawDevicePreparationUsesNativeWholeDiskUnmount()
    {
        string? unmountedDevice = null;
        var access = new MacRawDeviceAccess((deviceId, _) =>
        {
            unmountedDevice = deviceId;
            return Task.CompletedTask;
        });

        await access.PrepareAsync(SelectedDisk(size: 64_000), writing: true, CancellationToken.None);

        Assert.Equal("disk4", unmountedDevice);
    }

    [Fact]
    public async Task RawDeviceRestoreReliesOnDiskArbitrationRescan()
    {
        var unmountCalls = 0;
        var access = new MacRawDeviceAccess((_, _) =>
        {
            unmountCalls++;
            return Task.CompletedTask;
        });

        await access.RestoreAsync(SelectedDisk(size: 64_000), CancellationToken.None);

        Assert.Equal(0, unmountCalls);
    }

    private static MacDeviceCatalog CatalogWithDiskSize(long size) => new((_, arguments, _) =>
    {
        var values = arguments.ToArray();
        return Task.FromResult(values[0] == "list"
            ? new ProcessResult(0, PhysicalDiskList(size), string.Empty)
            : new ProcessResult(0, SystemDiskInfo(), string.Empty));
    });

    private static DeviceDescriptor SelectedDisk(long size) => new(
        "disk4",
        "/dev/disk4",
        "External USB disk",
        "serial-4",
        size,
        512,
        "USB",
        true,
        true,
        false,
        false,
        ["/Volumes/USB"]);

    private static string PhysicalDiskList(long size) => $$"""
        <?xml version="1.0" encoding="UTF-8"?>
        <plist version="1.0"><dict>
          <key>AllDisksAndPartitions</key><array><dict>
            <key>DeviceIdentifier</key><string>disk4</string>
            <key>Size</key><integer>{{size}}</integer>
            <key>Partitions</key><array><dict>
              <key>DeviceIdentifier</key><string>disk4s1</string>
              <key>MountPoint</key><string>/Volumes/USB</string>
            </dict></array>
          </dict></array>
        </dict></plist>
        """;

    private static string SystemDiskInfo(string systemDisk = "disk0") => $$"""
        <?xml version="1.0" encoding="UTF-8"?>
        <plist version="1.0"><dict>
          <key>DeviceIdentifier</key><string>{{systemDisk}}s1</string>
          <key>ParentWholeDisk</key><string>{{systemDisk}}</string>
          <key>APFSPhysicalStores</key><array><string>{{systemDisk}}</string></array>
        </dict></plist>
        """;
}
