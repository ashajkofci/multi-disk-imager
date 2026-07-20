using MultiDiskImager.Core;

namespace MultiDiskImager.Platform;

internal static class PlatformServices
{
    public static IBlockDeviceCatalog CreateCatalog(uint? macMetadataUserId = null)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsDeviceCatalog();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacDeviceCatalog(macMetadataUserId);
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxDeviceCatalog();
        }

        return new UnsupportedDeviceCatalog();
    }

    public static IRawDeviceAccess CreateRawAccess()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsRawDeviceAccess();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacRawDeviceAccess();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxRawDeviceAccess();
        }

        return new UnsupportedRawDeviceAccess();
    }
}

internal sealed class UnsupportedDeviceCatalog : IBlockDeviceCatalog
{
    public Task<IReadOnlyList<DeviceDescriptor>> GetDevicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DeviceDescriptor>>([]);

    public Task<DeviceDescriptor?> GetDeviceAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult<DeviceDescriptor?>(null);
}
