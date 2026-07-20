using System.IO.Pipes;
using System.Text.Json;
using MultiDiskImager.Core;
using MultiDiskImager.Infrastructure;
using MultiDiskImager.Platform;

namespace MultiDiskImager.Privileged;

internal static class PrivilegedHelperServer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<int> RunAsync(string pipeName)
    {
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(30_000).ConfigureAwait(false);
        using var reader = new StreamReader(pipe, leaveOpen: true);
        using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
        var requestLine = await reader.ReadLineAsync().ConfigureAwait(false);
        if (requestLine is null)
        {
            return 2;
        }

        var request = JsonSerializer.Deserialize<HelperRequest>(requestLine, JsonOptions)
            ?? throw new InvalidDataException("The helper request is empty.");
        using var cancellation = new CancellationTokenSource();
        var cancelMonitor = MonitorCancellationAsync(reader, cancellation);
        var writeLock = new SemaphoreSlim(1, 1);

        try
        {
            var progress = new InlineProgress<ImagingProgress>(value =>
            {
                try
                {
                    WriteEventAsync(writer, writeLock, new HelperEvent("progress", Progress: value), CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (IOException)
                {
                    cancellation.Cancel();
                }
            });

            var result = await ExecuteAsync(request, progress, cancellation.Token).ConfigureAwait(false);
            await WriteEventAsync(writer, writeLock, new HelperEvent("result", Result: result), CancellationToken.None).ConfigureAwait(false);
            return result.Success ? 0 : result.Canceled ? 3 : 1;
        }
        catch (Exception exception)
        {
            await WriteEventAsync(writer, writeLock, new HelperEvent("error", Message: exception.Message), CancellationToken.None).ConfigureAwait(false);
            return 1;
        }
        finally
        {
            cancellation.Cancel();
            try
            {
                await cancelMonitor.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private static async Task<ImagingJobResult> ExecuteAsync(
        HelperRequest request,
        IProgress<ImagingProgress> progress,
        CancellationToken cancellationToken)
    {
        var catalog = PlatformServices.CreateCatalog();
        var rawAccess = PlatformServices.CreateRawAccess();
        var devices = await ValidateDevicesAsync(catalog, request.Devices, cancellationToken).ConfigureAwait(false);
        var prepared = new List<DeviceDescriptor>();
        var streams = new Dictionary<string, Stream>(StringComparer.Ordinal);
        await using var inhibitor = await PlatformPowerInhibitor.AcquireAsync().ConfigureAwait(false);

        try
        {
            var writing = request.Operation is ImagingOperation.Write or ImagingOperation.Wipe;
            foreach (var device in devices)
            {
                await rawAccess.PrepareAsync(device, writing, cancellationToken).ConfigureAwait(false);
                prepared.Add(device);
                streams[device.Id] = rawAccess.Open(device, writing ? FileAccess.ReadWrite : FileAccess.Read);
            }

            return request.Operation switch
            {
                ImagingOperation.Read => await ReadAsync(request, devices, streams, progress, cancellationToken).ConfigureAwait(false),
                ImagingOperation.Write => await WriteAsync(request, devices, streams, progress, cancellationToken).ConfigureAwait(false),
                ImagingOperation.Verify => await VerifyAsync(request, devices, streams, progress, cancellationToken).ConfigureAwait(false),
                ImagingOperation.Wipe => await WipeAsync(devices, streams, progress, cancellationToken).ConfigureAwait(false),
                _ => throw new ArgumentOutOfRangeException(nameof(request.Operation))
            };
        }
        finally
        {
            foreach (var stream in streams.Values)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }

            foreach (var device in prepared)
            {
                try
                {
                    await rawAccess.RestoreAsync(device, CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                }
            }
        }
    }

    private static async Task<ImagingJobResult> ReadAsync(
        HelperRequest request,
        IReadOnlyList<DeviceDescriptor> devices,
        IReadOnlyDictionary<string, Stream> streams,
        IProgress<ImagingProgress> progress,
        CancellationToken cancellationToken)
    {
        if (devices.Count != 1)
        {
            throw new InvalidOperationException("Reading requires exactly one source device.");
        }

        var device = devices[0];
        var source = streams[device.Id];
        var byteCount = request.ByteCount ?? device.Size;
        if (request.OnlyAllocated)
        {
            var layout = await PartitionTableParser.ParseAsync(source, device.Size, device.LogicalSectorSize, cancellationToken).ConfigureAwait(false);
            byteCount = Math.Min(byteCount, layout.LastAllocatedByte);
            source.Position = 0;
        }

        EnsureFreeSpace(request.ImagePath, byteCount);
        await using var image = new FileStream(request.ImagePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous);
        var engine = new ImagingEngine();
        var result = await engine.CopyToDevicesAsync(
            source,
            new Dictionary<string, Stream> { [device.Id] = image },
            byteCount,
            device.LogicalSectorSize,
            ImagingOperation.Read,
            progress,
            cancellationToken).ConfigureAwait(false);
        image.SetLength(byteCount);

        if (request.VerifyAfter && result.Success)
        {
            image.Position = 0;
            source.Position = 0;
            return await engine.VerifyDevicesAsync(image, new Dictionary<string, Stream> { [device.Id] = source }, byteCount, progress, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private static async Task<ImagingJobResult> WriteAsync(
        HelperRequest request,
        IReadOnlyList<DeviceDescriptor> devices,
        IReadOnlyDictionary<string, Stream> streams,
        IProgress<ImagingProgress> progress,
        CancellationToken cancellationToken)
    {
        await using var image = new FileStream(request.ImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var smallest = devices.Min(device => device.Size);
        if (image.Length > smallest && !request.AllowCrop)
        {
            throw new InvalidOperationException($"The image is {ByteSize.Format(image.Length - smallest)} larger than the smallest selected device.");
        }

        var byteCount = Math.Min(request.ByteCount ?? image.Length, smallest);
        var sectorSize = devices.Max(device => device.LogicalSectorSize);
        var engine = new ImagingEngine();
        var result = await engine.CopyToDevicesAsync(image, streams, byteCount, sectorSize, ImagingOperation.Write, progress, cancellationToken).ConfigureAwait(false);

        if (request.VerifyAfter && result.Devices.Any(device => device.Success))
        {
            image.Position = 0;
            foreach (var stream in streams.Values)
            {
                stream.Position = 0;
            }

            var successful = streams.Where(pair => result.Devices.Any(device => device.DeviceId == pair.Key && device.Success))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            return await engine.VerifyDevicesAsync(image, successful, byteCount, progress, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private static async Task<ImagingJobResult> VerifyAsync(
        HelperRequest request,
        IReadOnlyList<DeviceDescriptor> devices,
        IReadOnlyDictionary<string, Stream> streams,
        IProgress<ImagingProgress> progress,
        CancellationToken cancellationToken)
    {
        await using var image = new FileStream(request.ImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var byteCount = request.ByteCount ?? Math.Min(image.Length, devices.Min(device => device.Size));
        return await new ImagingEngine().VerifyDevicesAsync(image, streams, byteCount, progress, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ImagingJobResult> WipeAsync(
        IReadOnlyList<DeviceDescriptor> devices,
        IReadOnlyDictionary<string, Stream> streams,
        IProgress<ImagingProgress> progress,
        CancellationToken cancellationToken)
    {
        var engine = new ImagingEngine();
        var results = new List<DeviceOperationResult>();
        for (var index = 0; index < devices.Count; index++)
        {
            var device = devices[index];
            try
            {
                await engine.QuickWipeAsync(streams[device.Id], device.Size, cancellationToken: cancellationToken).ConfigureAwait(false);
                results.Add(new DeviceOperationResult(device.Id, true, device.Size));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                results.Add(new DeviceOperationResult(device.Id, false, 0, exception.Message));
            }

            progress.Report(new ImagingProgress(ImagingOperation.Wipe, index + 1, devices.Count, 0, null, $"Wiped {device.Id}"));
        }

        return new ImagingJobResult(ImagingOperation.Wipe, false, results);
    }

    private static async Task<IReadOnlyList<DeviceDescriptor>> ValidateDevicesAsync(
        IBlockDeviceCatalog catalog,
        IReadOnlyList<DeviceDescriptor> expected,
        CancellationToken cancellationToken)
    {
        if (expected.Count == 0)
        {
            throw new InvalidOperationException("No devices were selected.");
        }

        var validated = new List<DeviceDescriptor>(expected.Count);
        foreach (var requested in expected)
        {
            var current = await catalog.GetDeviceAsync(requested.Id, cancellationToken).ConfigureAwait(false)
                ?? throw new IOException($"Device {requested.Id} is no longer connected.");
            if (current.IsSystem)
            {
                throw new UnauthorizedAccessException($"The system disk {current.Id} is never a valid target.");
            }

            if (!current.Path.Equals(requested.Path, StringComparison.OrdinalIgnoreCase) || current.Size != requested.Size ||
                (!string.IsNullOrWhiteSpace(requested.Serial) && !string.Equals(current.Serial, requested.Serial, StringComparison.OrdinalIgnoreCase)))
            {
                throw new IOException($"Device {requested.Id} changed after selection. Refresh and select it again.");
            }

            validated.Add(current);
        }

        return validated;
    }

    private static void EnsureFreeSpace(string imagePath, long required)
    {
        var fullPath = Path.GetFullPath(imagePath);
        var root = Path.GetPathRoot(fullPath) ?? throw new IOException("The image path has no filesystem root.");
        var available = new DriveInfo(root).AvailableFreeSpace;
        if (available < required)
        {
            throw new IOException($"Not enough free space. Required {ByteSize.Format(required)}, available {ByteSize.Format(available)}.");
        }
    }

    private static async Task MonitorCancellationAsync(StreamReader reader, CancellationTokenSource cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellation.Token).ConfigureAwait(false);
            if (line is null)
            {
                cancellation.Cancel();
                return;
            }

            var control = JsonSerializer.Deserialize<HelperControl>(line, JsonOptions);
            if (control?.Command.Equals("cancel", StringComparison.OrdinalIgnoreCase) == true)
            {
                cancellation.Cancel();
                return;
            }
        }
    }

    private static async Task WriteEventAsync(
        StreamWriter writer,
        SemaphoreSlim writeLock,
        HelperEvent value,
        CancellationToken cancellationToken)
    {
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(value, JsonOptions).AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private sealed class InlineProgress<T>(Action<T> callback) : IProgress<T>
    {
        public void Report(T value) => callback(value);
    }
}
