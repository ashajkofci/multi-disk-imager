using System.Buffers;
using System.Diagnostics;

namespace MultiDiskImager.Core;

public sealed class ImagingEngine(int bufferSize = 4 * 1024 * 1024)
{
    private readonly int _bufferSize = bufferSize > 0
        ? bufferSize
        : throw new ArgumentOutOfRangeException(nameof(bufferSize));

    public async Task<ImagingJobResult> CopyToDevicesAsync(
        Stream source,
        IReadOnlyDictionary<string, Stream> destinations,
        long byteCount,
        int sectorSize,
        ImagingOperation operation,
        IProgress<ImagingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destinations);
        if (!source.CanRead)
        {
            throw new ArgumentException("Source stream is not readable.", nameof(source));
        }

        if (destinations.Count == 0)
        {
            throw new ArgumentException("At least one destination is required.", nameof(destinations));
        }

        if (byteCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount));
        }

        if (sectorSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sectorSize));
        }

        var chunkSize = Math.Max(sectorSize, _bufferSize - (_bufferSize % sectorSize));
        var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        var active = destinations.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var results = new Dictionary<string, DeviceOperationResult>(StringComparer.Ordinal);
        var stopwatch = Stopwatch.StartNew();
        var lastReport = TimeSpan.Zero;
        long processed = 0;

        try
        {
            while (processed < byteCount && active.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var requested = (int)Math.Min(chunkSize, byteCount - processed);
                var read = await ReadExactlyUpToAsync(source, buffer.AsMemory(0, requested), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException($"Source ended after {processed} of {byteCount} bytes.");
                }

                var finalChunk = processed + read >= byteCount;
                var writeLength = finalChunk ? AlignUp(read, sectorSize) : read;
                if (writeLength > read)
                {
                    buffer.AsSpan(read, writeLength - read).Clear();
                }

                var writes = active.Select(pair => WriteOneAsync(pair.Key, pair.Value, buffer.AsMemory(0, writeLength), cancellationToken)).ToArray();
                var writeResults = await Task.WhenAll(writes).ConfigureAwait(false);
                foreach (var writeResult in writeResults.Where(result => result.Error is not null))
                {
                    active.Remove(writeResult.DeviceId);
                    results[writeResult.DeviceId] = new DeviceOperationResult(writeResult.DeviceId, false, processed, writeResult.Error!.Message);
                }

                processed += read;
                ReportProgress(operation, processed, byteCount, "Transferring", stopwatch, ref lastReport, progress);
            }

            foreach (var pair in active)
            {
                try
                {
                    await pair.Value.FlushAsync(cancellationToken).ConfigureAwait(false);
                    results[pair.Key] = new DeviceOperationResult(pair.Key, true, processed);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    results[pair.Key] = new DeviceOperationResult(pair.Key, false, processed, exception.Message);
                }
            }

            progress?.Report(CreateProgress(operation, processed, byteCount, "Complete", stopwatch));
            return new ImagingJobResult(operation, false, destinations.Keys.Select(id => results[id]).ToArray());
        }
        catch (OperationCanceledException)
        {
            foreach (var id in active.Keys)
            {
                results.TryAdd(id, new DeviceOperationResult(id, false, processed, "Canceled"));
            }

            return new ImagingJobResult(operation, true, destinations.Keys.Select(id => results[id]).ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    public async Task<ImagingJobResult> VerifyDevicesAsync(
        Stream image,
        IReadOnlyDictionary<string, Stream> devices,
        long byteCount,
        IProgress<ImagingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(devices);
        if (devices.Count == 0)
        {
            throw new ArgumentException("At least one device is required.", nameof(devices));
        }

        var imageBuffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        var deviceBuffers = devices.Keys.ToDictionary(id => id, _ => ArrayPool<byte>.Shared.Rent(_bufferSize), StringComparer.Ordinal);
        var active = devices.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var results = new Dictionary<string, DeviceOperationResult>(StringComparer.Ordinal);
        var stopwatch = Stopwatch.StartNew();
        var lastReport = TimeSpan.Zero;
        long processed = 0;

        try
        {
            while (processed < byteCount && active.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var requested = (int)Math.Min(_bufferSize, byteCount - processed);
                var imageRead = await ReadExactlyUpToAsync(image, imageBuffer.AsMemory(0, requested), cancellationToken).ConfigureAwait(false);
                if (imageRead != requested)
                {
                    throw new EndOfStreamException($"Image ended after {processed + imageRead} of {byteCount} bytes.");
                }

                foreach (var pair in active.ToArray())
                {
                    try
                    {
                        var deviceRead = await ReadExactlyUpToAsync(pair.Value, deviceBuffers[pair.Key].AsMemory(0, requested), cancellationToken).ConfigureAwait(false);
                        if (deviceRead != requested)
                        {
                            results[pair.Key] = new DeviceOperationResult(pair.Key, false, processed + deviceRead, "Device ended before the image.", processed + deviceRead);
                            active.Remove(pair.Key);
                            continue;
                        }

                        var mismatch = imageBuffer.AsSpan(0, requested).SequenceCompareTo(deviceBuffers[pair.Key].AsSpan(0, requested));
                        if (mismatch != 0)
                        {
                            var index = FirstMismatch(imageBuffer, deviceBuffers[pair.Key], requested);
                            results[pair.Key] = new DeviceOperationResult(pair.Key, false, processed + index, "Image data does not match the device.", processed + index);
                            active.Remove(pair.Key);
                        }
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        results[pair.Key] = new DeviceOperationResult(pair.Key, false, processed, exception.Message);
                        active.Remove(pair.Key);
                    }
                }

                processed += requested;
                ReportProgress(ImagingOperation.Verify, processed, byteCount, "Verifying", stopwatch, ref lastReport, progress);
            }

            foreach (var id in active.Keys)
            {
                results[id] = new DeviceOperationResult(id, true, processed);
            }

            progress?.Report(CreateProgress(ImagingOperation.Verify, processed, byteCount, "Complete", stopwatch));
            return new ImagingJobResult(ImagingOperation.Verify, false, devices.Keys.Select(id => results[id]).ToArray());
        }
        catch (OperationCanceledException)
        {
            foreach (var id in active.Keys)
            {
                results.TryAdd(id, new DeviceOperationResult(id, false, processed, "Canceled"));
            }

            return new ImagingJobResult(ImagingOperation.Verify, true, devices.Keys.Select(id => results[id]).ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(imageBuffer, clearArray: true);
            foreach (var buffer in deviceBuffers.Values)
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }
    }

    public async Task<bool> TailContainsNonZeroAsync(
        Stream image,
        long offset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (!image.CanSeek || !image.CanRead)
        {
            throw new ArgumentException("The image must be readable and seekable.", nameof(image));
        }

        if (offset < 0 || offset > image.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        image.Position = offset;
        var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        try
        {
            int read;
            while ((read = await image.ReadAsync(buffer.AsMemory(0, _bufferSize), cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (buffer.AsSpan(0, read).IndexOfAnyExcept((byte)0) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    public async Task QuickWipeAsync(
        Stream device,
        long deviceSize,
        int wipeRegionSize = 16 * 1024 * 1024,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!device.CanWrite || !device.CanSeek)
        {
            throw new ArgumentException("Device stream must be writable and seekable.", nameof(device));
        }

        if (deviceSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deviceSize));
        }

        var length = (int)Math.Min(wipeRegionSize, deviceSize);
        var zeros = ArrayPool<byte>.Shared.Rent(length);
        Array.Clear(zeros, 0, length);
        try
        {
            device.Position = 0;
            await device.WriteAsync(zeros.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
            if (deviceSize > length)
            {
                var tailStart = Math.Max(length, deviceSize - length);
                var tailLength = checked((int)Math.Min(length, deviceSize - tailStart));
                device.Position = tailStart;
                await device.WriteAsync(zeros.AsMemory(0, tailLength), cancellationToken).ConfigureAwait(false);
            }

            await device.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(zeros, clearArray: true);
        }
    }

    private static async Task<(string DeviceId, Exception? Error)> WriteOneAsync(
        string id,
        Stream stream,
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken)
    {
        try
        {
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            return (id, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return (id, exception);
        }
    }

    private static async Task<int> ReadExactlyUpToAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var count = await stream.ReadAsync(buffer[read..], cancellationToken).ConfigureAwait(false);
            if (count == 0)
            {
                break;
            }

            read += count;
        }

        return read;
    }

    private static int AlignUp(int value, int alignment) => checked(((value + alignment - 1) / alignment) * alignment);

    private static int FirstMismatch(byte[] left, byte[] right, int length)
    {
        for (var index = 0; index < length; index++)
        {
            if (left[index] != right[index])
            {
                return index;
            }
        }

        return length;
    }

    private static void ReportProgress(
        ImagingOperation operation,
        long processed,
        long total,
        string stage,
        Stopwatch stopwatch,
        ref TimeSpan lastReport,
        IProgress<ImagingProgress>? progress)
    {
        if (progress is null || (stopwatch.Elapsed - lastReport < TimeSpan.FromMilliseconds(200) && processed < total))
        {
            return;
        }

        lastReport = stopwatch.Elapsed;
        progress.Report(CreateProgress(operation, processed, total, stage, stopwatch));
    }

    private static ImagingProgress CreateProgress(ImagingOperation operation, long processed, long total, string stage, Stopwatch stopwatch)
    {
        var speed = stopwatch.Elapsed.TotalSeconds <= 0 ? 0 : processed / stopwatch.Elapsed.TotalSeconds;
        TimeSpan? remaining = speed <= 0 || total <= processed ? TimeSpan.Zero : TimeSpan.FromSeconds((total - processed) / speed);
        return new ImagingProgress(operation, processed, total, speed, remaining, stage);
    }
}
