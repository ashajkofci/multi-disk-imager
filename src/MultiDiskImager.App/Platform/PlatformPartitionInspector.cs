using System.Management;
using System.Runtime.Versioning;
using MultiDiskImager.Core;
using MultiDiskImager.Infrastructure;

namespace MultiDiskImager.Platform;

internal static class PlatformPartitionInspector
{
    public static Task<PartitionLayout> InspectAsync(DeviceDescriptor device, CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsWindows())
        {
            return InspectWindowsAsync(device, cancellationToken);
        }

        if (OperatingSystem.IsMacOS())
        {
            return InspectMacAsync(device, cancellationToken);
        }

        return Task.FromResult(new PartitionLayout(PartitionScheme.Raw, device.Size, device.LogicalSectorSize, []));
    }

    [SupportedOSPlatform("windows")]
    private static Task<PartitionLayout> InspectWindowsAsync(DeviceDescriptor device, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            var index = device.Id.Replace("PhysicalDrive", string.Empty, StringComparison.OrdinalIgnoreCase);
            var partitions = new List<PartitionDescriptor>();
            using var searcher = new ManagementObjectSearcher(
                $"SELECT Index, StartingOffset, Size, Type, Name FROM Win32_DiskPartition WHERE DiskIndex={index}");
            foreach (ManagementObject partition in searcher.Get())
            {
                partitions.Add(new PartitionDescriptor(
                    Convert.ToInt32(partition["Index"] ?? partitions.Count, System.Globalization.CultureInfo.InvariantCulture) + 1,
                    Convert.ToInt64(partition["StartingOffset"] ?? 0, System.Globalization.CultureInfo.InvariantCulture),
                    Convert.ToInt64(partition["Size"] ?? 0, System.Globalization.CultureInfo.InvariantCulture),
                    Convert.ToString(partition["Type"], System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown",
                    Convert.ToString(partition["Name"], System.Globalization.CultureInfo.InvariantCulture)));
            }

            var scheme = partitions.Any(partition => partition.Type.Contains("GPT", StringComparison.OrdinalIgnoreCase))
                ? PartitionScheme.Gpt
                : partitions.Count > 0 ? PartitionScheme.Mbr : PartitionScheme.Raw;
            return new PartitionLayout(scheme, device.Size, device.LogicalSectorSize, partitions);
        }, cancellationToken);

    [SupportedOSPlatform("macos")]
    private static async Task<PartitionLayout> InspectMacAsync(DeviceDescriptor device, CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync("/usr/sbin/diskutil", ["list", "-plist", device.Id], cancellationToken).ConfigureAwait(false);
        result.EnsureSuccess($"Inspecting {device.Id}");
        var root = MacPlist.Parse(result.StandardOutput);
        var whole = root.DictionaryArray("AllDisksAndPartitions").FirstOrDefault();
        var partitions = whole?.DictionaryArray("Partitions").Select((partition, index) => new PartitionDescriptor(
            index + 1,
            partition.Integer("Offset"),
            partition.Integer("Size"),
            partition.String("Content") ?? "Unknown",
            partition.String("VolumeName"))).ToArray() ?? [];
        var content = whole?.String("Content") ?? string.Empty;
        var scheme = content.Contains("GUID", StringComparison.OrdinalIgnoreCase)
            ? PartitionScheme.Gpt
            : partitions.Length > 0 ? PartitionScheme.Mbr : PartitionScheme.Raw;
        return new PartitionLayout(scheme, device.Size, device.LogicalSectorSize, partitions);
    }
}

