using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MultiDiskImager.Platform;

[SupportedOSPlatform("linux")]
internal static class LinuxPrivilege
{
    public static bool IsRoot => GetEffectiveUserId() == 0;

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint GetEffectiveUserId();
}
