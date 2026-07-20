namespace MultiDiskImager.Core;

public enum ImagingOperation
{
    Read,
    Write,
    Verify,
    Wipe
}

public enum PartitionScheme
{
    Raw,
    Mbr,
    Gpt
}

public sealed record DeviceDescriptor(
    string Id,
    string Path,
    string Model,
    string? Serial,
    long Size,
    int LogicalSectorSize,
    string BusType,
    bool IsRemovable,
    bool IsExternal,
    bool IsReadOnly,
    bool IsSystem,
    IReadOnlyList<string> MountPoints)
{
    public string DisplayName => $"{Model} — {ByteSize.Format(Size)} ({Id})";
}

public sealed record PartitionDescriptor(
    int Number,
    long StartByte,
    long Length,
    string Type,
    string? Name = null);

public sealed record PartitionLayout(
    PartitionScheme Scheme,
    long DiskSize,
    int SectorSize,
    IReadOnlyList<PartitionDescriptor> Partitions)
{
    public long LastAllocatedByte => Partitions.Count == 0
        ? DiskSize
        : Partitions.Max(partition => checked(partition.StartByte + partition.Length));
}

public sealed record ImagingProgress(
    ImagingOperation Operation,
    long BytesProcessed,
    long TotalBytes,
    double BytesPerSecond,
    TimeSpan? Remaining,
    string Stage)
{
    public double Fraction => TotalBytes <= 0 ? 0 : Math.Clamp((double)BytesProcessed / TotalBytes, 0, 1);
}

public sealed record DeviceOperationResult(
    string DeviceId,
    bool Success,
    long BytesProcessed,
    string? Error = null,
    long? FirstMismatchOffset = null);

public sealed record ImagingJobResult(
    ImagingOperation Operation,
    bool Canceled,
    IReadOnlyList<DeviceOperationResult> Devices)
{
    public bool Success => !Canceled && Devices.Count > 0 && Devices.All(device => device.Success);
    public bool PartialSuccess => !Canceled && Devices.Any(device => device.Success) && Devices.Any(device => !device.Success);
}

public static class ByteSize
{
    private static readonly string[] Units = ["B", "KiB", "MiB", "GiB", "TiB"];

    public static string Format(long bytes)
    {
        var value = Math.Abs((double)bytes);
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        if (bytes < 0)
        {
            value = -value;
        }

        return $"{value:0.##} {Units[unit]}";
    }
}

