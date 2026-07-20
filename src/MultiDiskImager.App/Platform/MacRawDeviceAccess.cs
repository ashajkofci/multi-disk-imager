using System.Runtime.Versioning;
using MultiDiskImager.Core;
using MultiDiskImager.Infrastructure;

namespace MultiDiskImager.Platform;

[SupportedOSPlatform("macos")]
internal sealed class MacRawDeviceAccess : IRawDeviceAccess
{
    public async Task PrepareAsync(DeviceDescriptor device, bool writing, CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync(
            "/usr/sbin/diskutil",
            ["unmountDisk", "force", $"/dev/{device.Id}"],
            cancellationToken).ConfigureAwait(false);
        result.EnsureSuccess($"Unmounting {device.Id}");
    }

    public Stream Open(DeviceDescriptor device, FileAccess access)
    {
        var rawPath = device.Path.Replace("/dev/disk", "/dev/rdisk", StringComparison.Ordinal);
        return new FileStream(
            rawPath,
            FileMode.Open,
            access,
            FileShare.ReadWrite,
            1024 * 1024,
            FileOptions.Asynchronous | (access == FileAccess.Write || access == FileAccess.ReadWrite ? FileOptions.WriteThrough : FileOptions.None));
    }

    public async Task RestoreAsync(DeviceDescriptor device, CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync(
            "/usr/sbin/diskutil",
            ["mountDisk", $"/dev/{device.Id}"],
            cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0 && !result.StandardError.Contains("no mountable file systems", StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException($"Unable to remount {device.Id}: {result.StandardError.Trim()}");
        }
    }
}

