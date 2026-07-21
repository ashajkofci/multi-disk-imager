using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using MultiDiskImager.Core;
using MultiDiskImager.Infrastructure;

namespace MultiDiskImager.Platform;

[SupportedOSPlatform("macos")]
internal sealed class MacDeviceCatalog : IBlockDeviceCatalog
{
    private readonly ProcessRunnerDelegate _runProcess;
    private readonly TimeSpan _diskUtilTimeout;
    private IReadOnlySet<string>? _systemDiskIds;

    public MacDeviceCatalog() : this(ProcessRunner.RunAsync, TimeSpan.FromSeconds(15))
    {
    }

    internal MacDeviceCatalog(ProcessRunnerDelegate runProcess, TimeSpan? diskUtilTimeout = null)
    {
        _runProcess = runProcess;
        _diskUtilTimeout = diskUtilTimeout ?? TimeSpan.FromSeconds(15);
    }

    public async Task<IReadOnlyList<DeviceDescriptor>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var listResult = await RunDiskUtilAsync(["list", "-plist", "physical"], "Disk enumeration", cancellationToken).ConfigureAwait(false);
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

            var infoResult = await RunDiskUtilAsync(["info", "-plist", id], $"Inspecting {id}", cancellationToken).ConfigureAwait(false);
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

            var mountPoints = disk.DescendantStrings("MountPoint");

            devices.Add(CreateDescriptor(info, id, disk.Integer("Size"), systemIds.Contains(id), mountPoints));
        }

        return devices.OrderBy(device => device.Id, StringComparer.Ordinal).ToArray();
    }

    public async Task<DeviceDescriptor?> GetDeviceAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!Regex.IsMatch(id, @"^disk\d+$", RegexOptions.CultureInvariant))
        {
            return null;
        }

        // Revalidation runs inside the privileged helper. Querying the complete
        // catalog here made every selected device repeat all diskutil calls and
        // allowed an unrelated disk to block an operation indefinitely.
        var infoResult = await RunDiskUtilAsync(["info", "-plist", id], $"Inspecting {id}", cancellationToken).ConfigureAwait(false);
        if (infoResult.ExitCode != 0)
        {
            return null;
        }

        var info = MacPlist.Parse(infoResult.StandardOutput);
        if (!info.Boolean("Whole", fallback: true) ||
            string.Equals(info.String("VirtualOrPhysical"), "Virtual", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var systemIds = await GetSystemDiskIdsAsync(cancellationToken).ConfigureAwait(false);
        var mountPoint = info.String("MountPoint");
        IReadOnlyList<string> mountPoints = string.IsNullOrWhiteSpace(mountPoint) ? [] : [mountPoint];
        return CreateDescriptor(info, id, 0, systemIds.Contains(id), mountPoints);
    }

    public async Task<IReadOnlyList<DeviceDescriptor>> ValidateSelectionsAsync(
        IReadOnlyList<DeviceDescriptor> expected,
        CancellationToken cancellationToken = default)
    {
        var listResult = await RunDiskUtilAsync(["list", "-plist", "physical"], "Disk validation", cancellationToken).ConfigureAwait(false);
        listResult.EnsureSuccess("Disk validation");
        var list = MacPlist.Parse(listResult.StandardOutput);
        var currentById = list.DictionaryArray("AllDisksAndPartitions")
            .Where(disk => !string.IsNullOrWhiteSpace(disk.String("DeviceIdentifier")))
            .ToDictionary(disk => disk.String("DeviceIdentifier")!, StringComparer.Ordinal);
        var systemIds = await GetSystemDiskIdsAsync(cancellationToken).ConfigureAwait(false);
        var validated = new List<DeviceDescriptor>(expected.Count);

        foreach (var requested in expected)
        {
            if (!currentById.TryGetValue(requested.Id, out var disk))
            {
                throw new IOException($"Device {requested.Id} is no longer connected.");
            }

            if (systemIds.Contains(requested.Id))
            {
                throw new UnauthorizedAccessException($"The system disk {requested.Id} is never a valid target.");
            }

            var currentSize = disk.Integer("Size");
            if (!requested.Path.Equals($"/dev/{requested.Id}", StringComparison.Ordinal) ||
                currentSize <= 0 || currentSize != requested.Size)
            {
                throw new IOException($"Device {requested.Id} changed after selection. Refresh and select it again.");
            }

            var mountPoints = disk.DescendantStrings("MountPoint");
            validated.Add(requested with { IsSystem = false, MountPoints = mountPoints });
        }

        return validated;
    }

    private async Task<IReadOnlySet<string>> GetSystemDiskIdsAsync(CancellationToken cancellationToken)
    {
        if (_systemDiskIds is not null)
        {
            return _systemDiskIds;
        }

        var systemIds = new HashSet<string>(StringComparer.Ordinal);
        var result = await RunDiskUtilAsync(["info", "-plist", "/"], "Identifying the system disk", cancellationToken).ConfigureAwait(false);
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

        _systemDiskIds = systemIds;
        return _systemDiskIds;
    }

    private static DeviceDescriptor CreateDescriptor(
        MacPlist info,
        string id,
        long fallbackSize,
        bool isSystem,
        IReadOnlyList<string> mountPoints)
    {
        var internalDisk = info.Boolean("Internal");
        var removable = info.Boolean("RemovableMedia") || info.Boolean("Ejectable");
        var protocol = info.String("BusProtocol") ?? info.String("Protocol") ?? "Unknown";
        return new DeviceDescriptor(
            id,
            info.String("DeviceNode") ?? $"/dev/{id}",
            info.String("MediaName") ?? info.String("IORegistryEntryName") ?? "Unknown disk",
            info.String("SerialNumber") ?? info.String("DiskUUID") ?? info.String("MediaUUID"),
            info.Integer("TotalSize", fallbackSize),
            checked((int)info.Integer("DeviceBlockSize", 512)),
            protocol,
            removable,
            !internalDisk || removable,
            !info.Boolean("Writable", fallback: true),
            isSystem,
            mountPoints);
    }

    private async Task<ProcessResult> RunDiskUtilAsync(
        IEnumerable<string> arguments,
        string operation,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_diskUtilTimeout);
        try
        {
            return await _runProcess("/usr/sbin/diskutil", arguments, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IOException($"{operation} timed out. Reconnect the disk and try again.");
        }
    }
}
