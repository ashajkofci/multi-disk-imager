using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using MultiDiskImager.Core;

namespace MultiDiskImager.Platform;

[SupportedOSPlatform("windows")]
internal sealed class WindowsRawDeviceAccess : IRawDeviceAccess
{
    private readonly Dictionary<string, List<SafeFileHandle>> _lockedVolumes = new(StringComparer.OrdinalIgnoreCase);
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint ShareRead = 0x00000001;
    private const uint ShareWrite = 0x00000002;
    private const uint OpenExisting = 3;
    private const uint FsctlLockVolume = 0x00090018;
    private const uint FsctlDismountVolume = 0x00090020;

    public Task PrepareAsync(DeviceDescriptor device, bool writing, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!writing)
        {
            return Task.CompletedTask;
        }

        var handles = new List<SafeFileHandle>();
        try
        {
            foreach (var mountPoint in device.MountPoints)
            {
                var volumePath = $@"\\.\{mountPoint.TrimEnd('\\')}";
                var handle = CreateFile(volumePath, GenericRead | GenericWrite, ShareRead | ShareWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
                if (handle.IsInvalid)
                {
                    handle.Dispose();
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Unable to open volume {mountPoint}.");
                }

                handles.Add(handle);
                if (!DeviceIoControl(handle, FsctlLockVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Unable to lock volume {mountPoint}. Close programs using it and try again.");
                }

                if (!DeviceIoControl(handle, FsctlDismountVolume, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Unable to dismount volume {mountPoint}.");
                }
            }

            _lockedVolumes[device.Id] = handles;
        }
        catch
        {
            handles.ForEach(handle => handle.Dispose());
            throw;
        }

        return Task.CompletedTask;
    }

    public Stream Open(DeviceDescriptor device, FileAccess access) => new FileStream(
        device.Path,
        FileMode.Open,
        access,
        FileShare.ReadWrite,
        1024 * 1024,
        FileOptions.Asynchronous | (access == FileAccess.Write || access == FileAccess.ReadWrite ? FileOptions.WriteThrough : FileOptions.None));

    public Task RestoreAsync(DeviceDescriptor device, CancellationToken cancellationToken)
    {
        if (_lockedVolumes.Remove(device.Id, out var handles))
        {
            handles.ForEach(handle => handle.Dispose());
        }

        return Task.CompletedTask;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(
        SafeFileHandle device,
        uint controlCode,
        IntPtr input,
        uint inputSize,
        IntPtr output,
        uint outputSize,
        out uint bytesReturned,
        IntPtr overlapped);
}
