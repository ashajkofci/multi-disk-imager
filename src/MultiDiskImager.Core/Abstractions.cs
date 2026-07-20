namespace MultiDiskImager.Core;

public interface IBlockDeviceCatalog
{
    Task<IReadOnlyList<DeviceDescriptor>> GetDevicesAsync(CancellationToken cancellationToken = default);
    Task<DeviceDescriptor?> GetDeviceAsync(string id, CancellationToken cancellationToken = default);
}

public interface IPowerInhibitor : IAsyncDisposable
{
}

public sealed class NullPowerInhibitor : IPowerInhibitor
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public interface IPowerInhibitorFactory
{
    ValueTask<IPowerInhibitor> AcquireAsync(string reason, CancellationToken cancellationToken = default);
}

