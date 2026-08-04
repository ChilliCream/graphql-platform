using System.Collections.Immutable;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Contains the signature manifest for a Fusion Archive, which includes file hashes
/// and metadata used for digital signature verification and integrity checking.
/// </summary>
public record SignatureManifest
{
    /// <summary>
    /// Gets or sets the version of the signature manifest format.
    /// Used to ensure compatibility with different signature verification implementations.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Gets or sets the hash algorithm used to compute file and manifest hashes.
    /// Currently, supports SHA256 for cryptographic integrity verification.
    /// </summary>
    public string Algorithm { get; init; } = "SHA256";

    /// <summary>
    /// Gets or sets the UTC timestamp when the signature was created.
    /// Provides an audit trail and helps detect replay attacks.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the dictionary of file paths to their computed hashes.
    /// The key is the file path within the archive, and the value is the hash
    /// in the format "algorithm:hexvalue" (e.g., "sha256:abc123...").
    /// Used to verify that files have not been modified since signing.
    /// </summary>
    public required ImmutableDictionary<string, string> Files { get; init; }

    /// <summary>
    /// Gets or sets the hash of the manifest itself (excluding this property).
    /// Used to verify the integrity of the manifest data during signature verification.
    /// Computed over the serialized manifest without the ManifestHash property.
    /// </summary>
    public string? ManifestHash { get; init; }
}
