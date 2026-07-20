using System.Runtime.Versioning;
using System.Text.Json;
using MultiDiskImager.Core;
using MultiDiskImager.Infrastructure;

namespace MultiDiskImager.Platform;

[SupportedOSPlatform("linux")]
internal sealed class LinuxDeviceCatalog : IBlockDeviceCatalog
{
    private static readonly string[] SystemMountPoints = ["/", "/boot", "/boot/efi"];

    public async Task<IReadOnlyList<DeviceDescriptor>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var result = await ProcessRunner.RunAsync(
            "/usr/bin/lsblk",
            ["--json", "--bytes", "--paths", "--output", "NAME,KNAME,PATH,MODEL,SERIAL,SIZE,LOG-SEC,TRAN,RM,HOTPLUG,RO,TYPE,MOUNTPOINTS"],
            cancellationToken).ConfigureAwait(false);
        result.EnsureSuccess("Block-device enumeration");

        using var document = JsonDocument.Parse(result.StandardOutput);
        if (!document.RootElement.TryGetProperty("blockdevices", out var devices))
        {
            return [];
        }

        var resultDevices = new List<DeviceDescriptor>();
        foreach (var device in devices.EnumerateArray())
        {
            if (!String(device, "type").Equals("disk", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var id = Path.GetFileName(String(device, "kname"));
            var path = String(device, "path");
            var size = Integer(device, "size");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(path) || size <= 0)
            {
                continue;
            }

            var mounts = DescendantsAndSelf(device)
                .SelectMany(MountPoints)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var transport = String(device, "tran");
            var removable = Boolean(device, "rm");
            var hotPlug = Boolean(device, "hotplug");
            var external = removable || hotPlug ||
                new[] { "usb", "firewire", "thunderbolt", "mmc", "sdio" }.Contains(transport, StringComparer.OrdinalIgnoreCase);
            var isSystem = mounts.Any(mount => SystemMountPoints.Contains(mount, StringComparer.Ordinal));

            resultDevices.Add(new DeviceDescriptor(
                id,
                path,
                NullIfEmpty(String(device, "model")) ?? "Unknown disk",
                NullIfEmpty(String(device, "serial")),
                size,
                checked((int)Math.Max(512, Integer(device, "log-sec", 512))),
                NullIfEmpty(transport) ?? "Unknown",
                removable,
                external,
                Boolean(device, "ro"),
                isSystem,
                mounts));
        }

        return resultDevices.OrderBy(device => device.Id, StringComparer.Ordinal).ToArray();
    }

    public async Task<DeviceDescriptor?> GetDeviceAsync(string id, CancellationToken cancellationToken = default) =>
        (await GetDevicesAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(device =>
            device.Id.Equals(id, StringComparison.Ordinal) || device.Path.Equals(id, StringComparison.Ordinal));

    internal static IEnumerable<JsonElement> DescendantsAndSelf(JsonElement device)
    {
        yield return device;
        if (!device.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var child in children.EnumerateArray())
        {
            foreach (var descendant in DescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }

    internal static IEnumerable<string> MountPoints(JsonElement device)
    {
        if (!device.TryGetProperty("mountpoints", out var mounts))
        {
            yield break;
        }

        if (mounts.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(mounts.GetString()))
        {
            yield return mounts.GetString()!;
        }
        else if (mounts.ValueKind == JsonValueKind.Array)
        {
            foreach (var mount in mounts.EnumerateArray())
            {
                if (mount.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(mount.GetString()))
                {
                    yield return mount.GetString()!;
                }
            }
        }
    }

    internal static string String(JsonElement value, string property) =>
        value.TryGetProperty(property, out var element) && element.ValueKind != JsonValueKind.Null
            ? element.ValueKind == JsonValueKind.String ? element.GetString()?.Trim() ?? string.Empty : element.ToString().Trim()
            : string.Empty;

    internal static long Integer(JsonElement value, string property, long fallback = 0)
    {
        if (!value.TryGetProperty(property, out var element))
        {
            return fallback;
        }

        return element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var number)
            ? number
            : long.TryParse(element.ToString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out number)
                ? number
                : fallback;
    }

    internal static bool Boolean(JsonElement value, string property)
    {
        if (!value.TryGetProperty(property, out var element))
        {
            return false;
        }

        return element.ValueKind == JsonValueKind.True ||
               element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number) && number != 0 ||
               element.ValueKind == JsonValueKind.String && (element.GetString() == "1" || bool.TryParse(element.GetString(), out var parsed) && parsed);
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
