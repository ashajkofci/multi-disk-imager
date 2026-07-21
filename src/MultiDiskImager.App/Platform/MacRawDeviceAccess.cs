using System.Runtime.Versioning;
using MultiDiskImager.Core;

namespace MultiDiskImager.Platform;

internal delegate Task MacDiskUnmountDelegate(string deviceId, CancellationToken cancellationToken);

[SupportedOSPlatform("macos")]
internal sealed class MacRawDeviceAccess : IRawDeviceAccess
{
    private readonly MacDiskUnmountDelegate _unmountDisk;

    public MacRawDeviceAccess() : this(MacDiskArbitration.UnmountWholeDiskAsync)
    {
    }

    internal MacRawDeviceAccess(MacDiskUnmountDelegate unmountDisk) => _unmountDisk = unmountDisk;

    public Task PrepareAsync(DeviceDescriptor device, bool writing, CancellationToken cancellationToken) =>
        _unmountDisk(device.Id, cancellationToken);

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

    public Task RestoreAsync(DeviceDescriptor device, CancellationToken cancellationToken)
    {
        // Closing the raw device lets Disk Arbitration rescan the partition
        // table and automatically remount any mountable volumes.
        return Task.CompletedTask;
    }
}
