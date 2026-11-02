using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Reads the extended access control list of a file on macOS.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class MacOSFileAcl
{
    private const string LibSystem = "/usr/lib/libSystem.dylib";
    private const int AclTypeExtended = 0x00000100;
    private const int AclFirstEntry = 0;
    private const int AclNextEntry = -1;
    private const int AclExtendedAllow = 1;
    private const int NoEntryError = 2;
    private const int GuidLength = 16;

    private static readonly int[] s_exposingPermissions =
    [
        1 << 1, // ACL_READ_DATA
        1 << 2, // ACL_WRITE_DATA
        1 << 5, // ACL_APPEND_DATA
        1 << 12, // ACL_WRITE_SECURITY
        1 << 13 // ACL_CHANGE_OWNER
    ];

    /// <summary>
    /// Whether no entry of the file's access control list grants an account other than the
    /// current user access to its contents, its access control list, or its ownership. False
    /// when the list cannot be read.
    /// </summary>
    public static bool GrantsOnlyCurrentUser(string path)
    {
        var acl = NativeMethods.acl_get_file(path, AclTypeExtended);

        if (acl == IntPtr.Zero)
        {
            // ENOENT reports a file without an access control list.
            return Marshal.GetLastPInvokeError() == NoEntryError;
        }

        try
        {
            var currentUser = new byte[GuidLength];

            if (NativeMethods.mbr_uid_to_uuid(NativeMethods.geteuid(), currentUser) != 0)
            {
                return false;
            }

            var entryId = AclFirstEntry;

            while (NativeMethods.acl_get_entry(acl, entryId, out var entry) == 0)
            {
                entryId = AclNextEntry;

                if (NativeMethods.acl_get_tag_type(entry, out var tag) != 0)
                {
                    return false;
                }

                if (tag != AclExtendedAllow)
                {
                    continue;
                }

                if (NativeMethods.acl_get_permset(entry, out var permissions) != 0)
                {
                    return false;
                }

                if (s_exposingPermissions.Any(permission => NativeMethods.acl_get_perm_np(permissions, permission) != 0)
                    && !IsQualifiedBy(entry, currentUser))
                {
                    return false;
                }
            }

            return true;
        }
        finally
        {
            _ = NativeMethods.acl_free(acl);
        }
    }

    private static bool IsQualifiedBy(IntPtr entry, byte[] user)
    {
        var qualifier = NativeMethods.acl_get_qualifier(entry);

        if (qualifier == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            var guid = new byte[GuidLength];
            Marshal.Copy(qualifier, guid, 0, GuidLength);
            return guid.AsSpan().SequenceEqual(user);
        }
        finally
        {
            _ = NativeMethods.acl_free(qualifier);
        }
    }

    private static class NativeMethods
    {
        [DllImport(LibSystem, SetLastError = true)]
        public static extern IntPtr acl_get_file([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int type);

        [DllImport(LibSystem)]
        public static extern int acl_get_entry(IntPtr acl, int entryId, out IntPtr entry);

        [DllImport(LibSystem)]
        public static extern int acl_get_tag_type(IntPtr entry, out int tag);

        [DllImport(LibSystem)]
        public static extern int acl_get_permset(IntPtr entry, out IntPtr permissions);

        [DllImport(LibSystem)]
        public static extern int acl_get_perm_np(IntPtr permissions, int permission);

        [DllImport(LibSystem)]
        public static extern IntPtr acl_get_qualifier(IntPtr entry);

        [DllImport(LibSystem)]
        public static extern int acl_free(IntPtr value);

        [DllImport(LibSystem)]
        public static extern uint geteuid();

        [DllImport(LibSystem)]
        public static extern int mbr_uid_to_uuid(uint uid, [Out] byte[] uuid);
    }
}
