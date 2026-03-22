using System.IO.Compression;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Specifies the mode for opening or creating a Fusion Archive.
/// </summary>
public enum FusionArchiveMode
{
    /// <summary>
    /// Opens an existing archive for reading only. No modifications are allowed.
    /// The archive must already exist and contain valid data.
    /// </summary>
    Read = ZipArchiveMode.Read,

    /// <summary>
    /// Creates a new archive for writing. If the target already exists, it will be overwritten.
    /// </summary>
    Create = ZipArchiveMode.Create,

    /// <summary>
    /// Opens an existing archive for both reading and writing. Allows modification of
    /// existing entries and addition of new entries. The archive must already exist.
    /// Use this mode when you need to modify an existing archive.
    /// </summary>
    Update = ZipArchiveMode.Update
}
