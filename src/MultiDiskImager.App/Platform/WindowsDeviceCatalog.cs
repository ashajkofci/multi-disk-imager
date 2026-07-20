using System.Management;
using System.Runtime.Versioning;
using MultiDiskImager.Core;

namespace MultiDiskImager.Platform;

[SupportedOSPlatform("windows")]
internal sealed class WindowsDeviceCatalog : IBlockDeviceCatalog
{
    public Task<IReadOnlyList<DeviceDescriptor>> GetDevicesAsync(CancellationToken cancellationToken = default) =>
        Task.Run<IReadOnlyList<DeviceDescriptor>>(() => Enumerate(), cancellationToken);

    public async Task<DeviceDescriptor?> GetDeviceAsync(string id, CancellationToken cancellationToken = default) =>
        (await GetDevicesAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(device =>
            device.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<DeviceDescriptor> Enumerate()
    {
        var systemDisk = FindSystemDiskIndex();
        var devices = new List<DeviceDescriptor>();
        using var searcher = new ManagementObjectSearcher(
            "SELECT Index, DeviceID, Model, SerialNumber, Size, BytesPerSector, InterfaceType, MediaType FROM Win32_DiskDrive");

        foreach (ManagementObject disk in searcher.Get())
        {
            var index = Convert.ToInt32(disk["Index"], System.Globalization.CultureInfo.InvariantCulture);
            var path = Convert.ToString(disk["DeviceID"], System.Globalization.CultureInfo.InvariantCulture) ?? $@"\\.\PhysicalDrive{index}";
            var model = (Convert.ToString(disk["Model"], System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown disk").Trim();
            var serial = Convert.ToString(disk["SerialNumber"], System.Globalization.CultureInfo.InvariantCulture)?.Trim();
            var size = Convert.ToInt64(disk["Size"] ?? 0, System.Globalization.CultureInfo.InvariantCulture);
            var sectorSize = Convert.ToInt32(disk["BytesPerSector"] ?? 512, System.Globalization.CultureInfo.InvariantCulture);
            var bus = (Convert.ToString(disk["InterfaceType"], System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown").Trim();
            var media = Convert.ToString(disk["MediaType"], System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            var mounts = FindMountPoints(disk);
            var removable = media.Contains("Removable", StringComparison.OrdinalIgnoreCase);
            var external = removable || bus.Equals("USB", StringComparison.OrdinalIgnoreCase) || bus.Equals("1394", StringComparison.OrdinalIgnoreCase);

            devices.Add(new DeviceDescriptor(
                $"PhysicalDrive{index}",
                path,
                model,
                serial,
                size,
                Math.Max(512, sectorSize),
                bus,
                removable,
                external,
                IsReadOnly(mounts),
                index == systemDisk,
                mounts));
        }

        return devices.OrderBy(device => device.Id, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static int? FindSystemDiskIndex()
    {
        var systemRoot = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\') ?? "C:";
        using var partitionSearcher = new ManagementObjectSearcher(
            $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{systemRoot.Replace("'", "''", StringComparison.Ordinal)}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
        foreach (ManagementObject partition in partitionSearcher.Get())
        {
            var partitionId = Convert.ToString(partition["DeviceID"], System.Globalization.CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(partitionId))
            {
                continue;
            }

            using var diskSearcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partitionId.Replace("'", "''", StringComparison.Ordinal)}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
            foreach (ManagementObject disk in diskSearcher.Get())
            {
                return Convert.ToInt32(disk["Index"], System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    private static IReadOnlyList<string> FindMountPoints(ManagementObject disk)
    {
        var result = new List<string>();
        foreach (ManagementObject partition in disk.GetRelated("Win32_DiskPartition"))
        {
            foreach (ManagementObject logicalDisk in partition.GetRelated("Win32_LogicalDisk"))
            {
                var id = Convert.ToString(logicalDisk["DeviceID"], System.Globalization.CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    result.Add(id);
                }
            }
        }

        return result;
    }

    private static bool IsReadOnly(IEnumerable<string> mountPoints)
    {
        foreach (var mountPoint in mountPoints)
        {
            try
            {
                var root = new DriveInfo(mountPoint);
                if (root.IsReady && (root.DriveFormat.Equals("CDFS", StringComparison.OrdinalIgnoreCase) ||
                                     root.DriveFormat.Equals("UDF", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return false;
    }
}

