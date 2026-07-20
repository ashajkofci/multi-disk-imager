using System.Management;
using System.Runtime.Versioning;
using System.Text.Json;
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

        if (OperatingSystem.IsLinux())
        {
            return InspectLinuxAsync(device, cancellationToken);
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

    [SupportedOSPlatform("linux")]
    private static async Task<PartitionLayout> InspectLinuxAsync(DeviceDescriptor device, CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync(
            "/usr/bin/lsblk",
            ["--json", "--bytes", "--paths", "--output", "PATH,TYPE,SIZE,PARTTYPE,PARTLABEL,PTTYPE", device.Path],
            cancellationToken).ConfigureAwait(false);
        result.EnsureSuccess($"Inspecting {device.Id}");
        using var document = JsonDocument.Parse(result.StandardOutput);
        if (!document.RootElement.TryGetProperty("blockdevices", out var roots) || roots.GetArrayLength() == 0)
        {
            return new PartitionLayout(PartitionScheme.Raw, device.Size, device.LogicalSectorSize, []);
        }

        var root = roots[0];
        var partitions = LinuxDeviceCatalog.DescendantsAndSelf(root)
            .Where(node => LinuxDeviceCatalog.String(node, "type").Equals("part", StringComparison.OrdinalIgnoreCase))
            .Select((partition, index) => new PartitionDescriptor(
                index + 1,
                ReadLinuxPartitionStart(partition),
                LinuxDeviceCatalog.Integer(partition, "size"),
                LinuxDeviceCatalog.String(partition, "parttype") is { Length: > 0 } type ? type : "Unknown",
                LinuxDeviceCatalog.String(partition, "partlabel") is { Length: > 0 } label ? label : null))
            .ToArray();
        var tableType = LinuxDeviceCatalog.String(root, "pttype");
        var scheme = tableType.Equals("gpt", StringComparison.OrdinalIgnoreCase)
            ? PartitionScheme.Gpt
            : partitions.Length > 0 ? PartitionScheme.Mbr : PartitionScheme.Raw;
        return new PartitionLayout(scheme, device.Size, device.LogicalSectorSize, partitions);
    }

    [SupportedOSPlatform("linux")]
    private static long ReadLinuxPartitionStart(JsonElement partition)
    {
        try
        {
            var name = Path.GetFileName(LinuxDeviceCatalog.String(partition, "path"));
            var startPath = Path.Combine("/sys/class/block", name, "start");
            return File.Exists(startPath) && long.TryParse(
                File.ReadAllText(startPath).Trim(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var sectors)
                ? checked(sectors * 512)
                : 0;
        }
        catch (IOException)
        {
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }
}
