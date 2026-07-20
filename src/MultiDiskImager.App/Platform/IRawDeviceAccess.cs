using MultiDiskImager.Core;

namespace MultiDiskImager.Platform;

internal interface IRawDeviceAccess
{
    Task PrepareAsync(DeviceDescriptor device, bool writing, CancellationToken cancellationToken);
    Stream Open(DeviceDescriptor device, FileAccess access);
    Task RestoreAsync(DeviceDescriptor device, CancellationToken cancellationToken);
}

internal sealed class UnsupportedRawDeviceAccess : IRawDeviceAccess
{
    public Task PrepareAsync(DeviceDescriptor device, bool writing, CancellationToken cancellationToken) =>
        throw new PlatformNotSupportedException("Raw disk access is supported on Windows, macOS, and Linux only.");

    public Stream Open(DeviceDescriptor device, FileAccess access) =>
        throw new PlatformNotSupportedException("Raw disk access is supported on Windows, macOS, and Linux only.");

    public Task RestoreAsync(DeviceDescriptor device, CancellationToken cancellationToken) => Task.CompletedTask;
}
