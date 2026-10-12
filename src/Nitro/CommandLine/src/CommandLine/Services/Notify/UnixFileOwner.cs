using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Reads the owner of a file on Unix.
/// </summary>
[UnsupportedOSPlatform("windows")]
internal static class UnixFileOwner
{
    private const string SystemNative = "libSystem.Native";

    /// <summary>
    /// Whether the effective user of this process owns the entry at <paramref name="path"/>,
    /// without following a final symbolic link. False when the entry cannot be read.
    /// </summary>
    public static bool IsCurrentUser(string path)
        => NativeMethods.LStat(path, out var status) == 0 && status.Uid == NativeMethods.GetEUid();

    // The leading fields of the runtime's FileStatus, whose layout has only grown at its end;
    // the size leaves room for every later field.
    [StructLayout(LayoutKind.Sequential, Size = 256)]
    private struct FileStatus
    {
        public int Flags;
        public int Mode;
        public uint Uid;
        public uint Gid;
    }

    private static class NativeMethods
    {
        [DllImport(SystemNative, EntryPoint = "SystemNative_LStat", SetLastError = true)]
        public static extern int LStat([MarshalAs(UnmanagedType.LPUTF8Str)] string path, out FileStatus output);

        [DllImport(SystemNative, EntryPoint = "SystemNative_GetEUid")]
        public static extern uint GetEUid();
    }
}
