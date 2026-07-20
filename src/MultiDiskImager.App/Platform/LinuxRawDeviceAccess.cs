using System.Runtime.Versioning;
using System.Text.Json;
using MultiDiskImager.Core;
using MultiDiskImager.Infrastructure;

namespace MultiDiskImager.Platform;

[SupportedOSPlatform("linux")]
internal sealed class LinuxRawDeviceAccess : IRawDeviceAccess
{
    private readonly Dictionary<string, IReadOnlyList<MountedFileSystem>> _unmounted = new(StringComparer.Ordinal);
    private readonly HashSet<string> _writtenDevices = new(StringComparer.Ordinal);

    public async Task PrepareAsync(DeviceDescriptor device, bool writing, CancellationToken cancellationToken)
    {
        var mounts = await GetMountedFileSystemsAsync(device, cancellationToken).ConfigureAwait(false);
        if (mounts.Any(mount => mount.MountPoint is "/" or "/boot" or "/boot/efi"))
        {
            throw new UnauthorizedAccessException($"The system disk {device.Id} cannot be opened for raw access.");
        }

        var completed = new List<MountedFileSystem>();
        try
        {
            foreach (var mount in mounts.OrderByDescending(value => value.MountPoint.Length))
            {
                var result = await ProcessRunner.RunAsync("/usr/bin/umount", ["--", mount.MountPoint], cancellationToken).ConfigureAwait(false);
                result.EnsureSuccess($"Unmounting {mount.MountPoint}");
                completed.Add(mount);
            }
        }
        catch
        {
            await RestoreMountsAsync(completed, CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        _unmounted[device.Id] = completed;
        if (writing)
        {
            _writtenDevices.Add(device.Id);
        }
    }

    public Stream Open(DeviceDescriptor device, FileAccess access) => new FileStream(
        device.Path,
        FileMode.Open,
        access,
        FileShare.ReadWrite,
        1024 * 1024,
        FileOptions.Asynchronous | (access == FileAccess.Write || access == FileAccess.ReadWrite ? FileOptions.WriteThrough : FileOptions.None));

    public async Task RestoreAsync(DeviceDescriptor device, CancellationToken cancellationToken)
    {
        if (_writtenDevices.Remove(device.Id))
        {
            await TryRunAsync("/usr/sbin/blockdev", ["--rereadpt", device.Path], cancellationToken).ConfigureAwait(false);
            await TryRunAsync("/usr/bin/udevadm", ["settle"], cancellationToken).ConfigureAwait(false);
        }

        if (_unmounted.Remove(device.Id, out var mounts))
        {
            await RestoreMountsAsync(mounts, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<IReadOnlyList<MountedFileSystem>> GetMountedFileSystemsAsync(
        DeviceDescriptor device,
        CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync(
            "/usr/bin/lsblk",
            ["--json", "--paths", "--output", "PATH,TYPE,MOUNTPOINTS", device.Path],
            cancellationToken).ConfigureAwait(false);
        result.EnsureSuccess($"Inspecting mounts on {device.Id}");
        using var document = JsonDocument.Parse(result.StandardOutput);
        if (!document.RootElement.TryGetProperty("blockdevices", out var devices))
        {
            return [];
        }

        var mounts = new List<MountedFileSystem>();
        foreach (var root in devices.EnumerateArray())
        {
            foreach (var node in LinuxDeviceCatalog.DescendantsAndSelf(root))
            {
                var path = LinuxDeviceCatalog.String(node, "path");
                mounts.AddRange(LinuxDeviceCatalog.MountPoints(node).Select(mount => new MountedFileSystem(path, mount)));
            }
        }

        return mounts.Where(mount => !string.IsNullOrWhiteSpace(mount.DevicePath))
            .DistinctBy(mount => mount.MountPoint, StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task RestoreMountsAsync(IEnumerable<MountedFileSystem> mounts, CancellationToken cancellationToken)
    {
        foreach (var mount in mounts.OrderBy(value => value.MountPoint.Length))
        {
            if (!File.Exists(mount.DevicePath))
            {
                continue;
            }

            await TryRunAsync("/usr/bin/mount", ["--", mount.DevicePath, mount.MountPoint], cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task TryRunAsync(string fileName, IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        try
        {
            _ = await ProcessRunner.RunAsync(fileName, arguments, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
        }
    }

    private sealed record MountedFileSystem(string DevicePath, string MountPoint);
}
