using System.Collections.Immutable;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Describes the full logical content of a Fusion Archive by recording a digest for every file
/// and an aggregate digest for every artifact. The content manifest establishes the identity and
/// integrity of the archive contents, while an optional digital signature establishes their
/// authenticity.
/// </summary>
public sealed record ArchiveManifest
{
    /// <summary>
    /// Gets the manifest schema version. It is versioned independently of the archive format version.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Gets the digest algorithm used for all entries. <c>sha256</c> is the only supported value.
    /// </summary>
    public string Algorithm { get; init; } = "sha256";

    /// <summary>
    /// Gets the map of each archive-relative file path to the digest of that file. Digests are
    /// rendered as <c>sha256:</c> followed by the lowercase hexadecimal digest of the file bytes.
    /// </summary>
    public required ImmutableSortedDictionary<string, string> Files { get; init; }

    /// <summary>
    /// Gets the map of each artifact key to the aggregate digest of the artifact's member files.
    /// An artifact is a logical unit of one or more files that is loaded, compiled, or cached as a whole.
    /// </summary>
    public required ImmutableSortedDictionary<string, string> Artifacts { get; init; }
}
