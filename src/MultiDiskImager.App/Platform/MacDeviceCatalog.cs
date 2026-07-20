using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using MultiDiskImager.Core;
using MultiDiskImager.Infrastructure;

namespace MultiDiskImager.Platform;

[SupportedOSPlatform("macos")]
internal sealed class MacDeviceCatalog : IBlockDeviceCatalog
{
    public async Task<IReadOnlyList<DeviceDescriptor>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var listResult = await ProcessRunner.RunAsync("/usr/sbin/diskutil", ["list", "-plist", "physical"], cancellationToken).ConfigureAwait(false);
        listResult.EnsureSuccess("Disk enumeration");
        var list = MacPlist.Parse(listResult.StandardOutput);
        var systemIds = await GetSystemDiskIdsAsync(cancellationToken).ConfigureAwait(false);
        var devices = new List<DeviceDescriptor>();

        foreach (var disk in list.DictionaryArray("AllDisksAndPartitions"))
        {
            var id = disk.String("DeviceIdentifier");
            if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("disk", StringComparison.Ordinal))
            {
                continue;
            }

            var infoResult = await ProcessRunner.RunAsync("/usr/sbin/diskutil", ["info", "-plist", id], cancellationToken).ConfigureAwait(false);
            if (infoResult.ExitCode != 0)
            {
                continue;
            }

            var info = MacPlist.Parse(infoResult.StandardOutput);
            if (!info.Boolean("Whole", fallback: true) ||
                string.Equals(info.String("VirtualOrPhysical"), "Virtual", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var internalDisk = info.Boolean("Internal");
            var removable = info.Boolean("RemovableMedia") || info.Boolean("Ejectable");
            var protocol = info.String("BusProtocol") ?? info.String("Protocol") ?? "Unknown";
            var external = !internalDisk || removable;
            var mountPoints = disk.DictionaryArray("Partitions")
                .Select(partition => partition.String("MountPoint"))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToArray();

            devices.Add(new DeviceDescriptor(
                id,
                info.String("DeviceNode") ?? $"/dev/{id}",
                info.String("MediaName") ?? info.String("IORegistryEntryName") ?? "Unknown disk",
                info.String("SerialNumber") ?? info.String("DiskUUID") ?? info.String("MediaUUID"),
                info.Integer("TotalSize", disk.Integer("Size")),
                checked((int)info.Integer("DeviceBlockSize", 512)),
                protocol,
                removable,
                external,
                !info.Boolean("Writable", fallback: true),
                systemIds.Contains(id),
                mountPoints));
        }

        return devices.OrderBy(device => device.Id, StringComparer.Ordinal).ToArray();
    }

    public async Task<DeviceDescriptor?> GetDeviceAsync(string id, CancellationToken cancellationToken = default) =>
        (await GetDevicesAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(device => device.Id.Equals(id, StringComparison.Ordinal));

    private static async Task<IReadOnlySet<string>> GetSystemDiskIdsAsync(CancellationToken cancellationToken)
    {
        var systemIds = new HashSet<string>(StringComparer.Ordinal);
        var result = await ProcessRunner.RunAsync("/usr/sbin/diskutil", ["info", "-plist", "/"], cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            return systemIds;
        }

        var info = MacPlist.Parse(result.StandardOutput);
        var identifiers = new[] { info.String("ParentWholeDisk"), info.String("DeviceIdentifier") }
            .Concat(info.StringArray("APFSPhysicalStores"));
        foreach (var identifier in identifiers.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var wholeDisk = Regex.Match(identifier!, @"^disk\d+", RegexOptions.CultureInvariant).Value;
            if (!string.IsNullOrWhiteSpace(wholeDisk))
            {
                systemIds.Add(wholeDisk);
            }
        }

        return systemIds;
    }
}
