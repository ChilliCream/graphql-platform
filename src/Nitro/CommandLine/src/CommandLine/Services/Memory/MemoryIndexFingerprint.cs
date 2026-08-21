using System.Security.Cryptography;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Computes a cheap fingerprint of a curated directory's markdown files,
/// used to detect whether a search index is stale without re-parsing every
/// file. Changing, adding, or removing a curated file changes its name,
/// last-write time, or length, any of which changes the fingerprint.
/// </summary>
internal static class MemoryIndexFingerprint
{
    /// <summary>
    /// The fingerprint of a curated directory that does not exist, distinct
    /// from any fingerprint an existing directory (empty or not) can
    /// produce.
    /// </summary>
    public const string Missing = "missing";

    public static string Compute(IFileSystem fileSystem, string curatedDirectory)
    {
        if (!fileSystem.DirectoryExists(curatedDirectory))
        {
            return Missing;
        }

        var builder = new StringBuilder();

        foreach (var path in fileSystem
            .GetFiles(curatedDirectory, "*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            var info = new FileInfo(path);

            builder
                .Append(Path.GetFileName(path)).Append('|')
                .Append(info.LastWriteTimeUtc.Ticks).Append('|')
                .Append(info.Length).Append('\n');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
