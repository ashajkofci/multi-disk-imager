using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MultiDiskImager.Platform;

[SupportedOSPlatform("macos")]
internal static class MacDiskArbitration
{
    private const string DiskArbitrationLibrary = "/System/Library/Frameworks/DiskArbitration.framework/DiskArbitration";
    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const uint UnmountWhole = 0x00000001;
    private const uint UnmountForce = 0x00080000;
    private const uint Utf8Encoding = 0x08000100;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
    private static readonly DADiskUnmountCallback UnmountCallback = OnUnmountCompleted;

    public static Task UnmountWholeDiskAsync(string deviceId, CancellationToken cancellationToken) =>
        Task.Run(() => UnmountWholeDisk(deviceId, cancellationToken), cancellationToken);

    private static void UnmountWholeDisk(string deviceId, CancellationToken cancellationToken)
    {
        var session = DASessionCreate(IntPtr.Zero);
        if (session == IntPtr.Zero)
        {
            throw new IOException("Unable to create a macOS Disk Arbitration session.");
        }

        var disk = IntPtr.Zero;
        var runLoopMode = IntPtr.Zero;
        GCHandle contextHandle = default;
        try
        {
            disk = DADiskCreateFromBSDName(IntPtr.Zero, session, deviceId);
            if (disk == IntPtr.Zero)
            {
                throw new IOException($"Device {deviceId} is no longer connected.");
            }

            runLoopMode = CFStringCreateWithCString(IntPtr.Zero, "kCFRunLoopDefaultMode", Utf8Encoding);
            if (runLoopMode == IntPtr.Zero)
            {
                throw new IOException("Unable to create the Disk Arbitration run-loop mode.");
            }

            var state = new UnmountState();
            contextHandle = GCHandle.Alloc(state);
            var runLoop = CFRunLoopGetCurrent();
            DASessionScheduleWithRunLoop(session, runLoop, runLoopMode);
            try
            {
                DADiskUnmount(
                    disk,
                    UnmountWhole | UnmountForce,
                    UnmountCallback,
                    GCHandle.ToIntPtr(contextHandle));

                var stopwatch = Stopwatch.StartNew();
                while (!state.Completed)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (stopwatch.Elapsed >= Timeout)
                    {
                        throw new IOException($"Timed out while unmounting {deviceId}. Close applications using the disk and try again.");
                    }

                    _ = CFRunLoopRunInMode(runLoopMode, 0.1, true);
                }

                if (state.Status != 0)
                {
                    throw new IOException($"Unable to unmount {deviceId} (Disk Arbitration status 0x{state.Status:x8}). Close applications using the disk and try again.");
                }
            }
            finally
            {
                DASessionUnscheduleFromRunLoop(session, runLoop, runLoopMode);
            }
        }
        finally
        {
            if (contextHandle.IsAllocated)
            {
                contextHandle.Free();
            }

            if (runLoopMode != IntPtr.Zero)
            {
                CFRelease(runLoopMode);
            }

            if (disk != IntPtr.Zero)
            {
                CFRelease(disk);
            }

            CFRelease(session);
        }
    }

    private static void OnUnmountCompleted(IntPtr disk, IntPtr dissenter, IntPtr context)
    {
        var handle = GCHandle.FromIntPtr(context);
        if (handle.Target is not UnmountState state)
        {
            return;
        }

        state.Status = dissenter == IntPtr.Zero ? 0 : DADissenterGetStatus(dissenter);
        state.Completed = true;
    }

    private sealed class UnmountState
    {
        public volatile bool Completed;
        public uint Status;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DADiskUnmountCallback(IntPtr disk, IntPtr dissenter, IntPtr context);

    [DllImport(DiskArbitrationLibrary)]
    private static extern IntPtr DASessionCreate(IntPtr allocator);

    [DllImport(DiskArbitrationLibrary, CharSet = CharSet.Ansi)]
    private static extern IntPtr DADiskCreateFromBSDName(IntPtr allocator, IntPtr session, string name);

    [DllImport(DiskArbitrationLibrary)]
    private static extern void DASessionScheduleWithRunLoop(IntPtr session, IntPtr runLoop, IntPtr runLoopMode);

    [DllImport(DiskArbitrationLibrary)]
    private static extern void DASessionUnscheduleFromRunLoop(IntPtr session, IntPtr runLoop, IntPtr runLoopMode);

    [DllImport(DiskArbitrationLibrary)]
    private static extern void DADiskUnmount(
        IntPtr disk,
        uint options,
        DADiskUnmountCallback callback,
        IntPtr context);

    [DllImport(DiskArbitrationLibrary)]
    private static extern uint DADissenterGetStatus(IntPtr dissenter);

    [DllImport(CoreFoundationLibrary)]
    private static extern IntPtr CFRunLoopGetCurrent();

    [DllImport(CoreFoundationLibrary)]
    private static extern int CFRunLoopRunInMode(
        IntPtr mode,
        double seconds,
        [MarshalAs(UnmanagedType.I1)] bool returnAfterSourceHandled);

    [DllImport(CoreFoundationLibrary, CharSet = CharSet.Ansi)]
    private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, string value, uint encoding);

    [DllImport(CoreFoundationLibrary)]
    private static extern void CFRelease(IntPtr value);
}
