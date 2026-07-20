using MultiDiskImager.Core;

namespace MultiDiskImager.Privileged;

internal sealed record HelperRequest(
    ImagingOperation Operation,
    string ImagePath,
    IReadOnlyList<DeviceDescriptor> Devices,
    bool VerifyAfter,
    bool OnlyAllocated,
    bool AllowCrop,
    long? ByteCount = null,
    uint? MetadataUserId = null);

internal sealed record HelperEvent(
    string Type,
    ImagingProgress? Progress = null,
    ImagingJobResult? Result = null,
    string? Message = null,
    PartitionLayout? Layout = null);

internal sealed record HelperControl(string Command);
