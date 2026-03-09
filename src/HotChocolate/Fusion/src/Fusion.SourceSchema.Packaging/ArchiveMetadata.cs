namespace HotChocolate.Fusion.SourceSchema.Packaging;

/// <summary>
/// Contains metadata about a Fusion Source schema archive.
/// </summary>
public record ArchiveMetadata
{
    /// <summary>
    /// Gets or sets the version of the Fusion source schema archive format specification.
    /// Used to ensure compatibility between different versions of tooling.
    /// </summary>
    public Version FormatVersion { get; init; } = new("1.0.0");
}
