using System.Collections.Immutable;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Contains metadata about a Fusion Archive, describing its format version,
/// supported gateway formats, and included source schemas.
/// </summary>
public record ArchiveMetadata
{
    /// <summary>
    /// Gets or sets the version of the Fusion Archive format specification.
    /// Used to ensure compatibility between different versions of tooling.
    /// </summary>
    public Version FormatVersion { get; init; } = new("1.0.0");

    /// <summary>
    /// Gets or sets the list of gateway format versions contained in this archive.
    /// Multiple versions allow gradual migration and compatibility testing.
    /// The gateway will select the highest compatible version at runtime.
    /// </summary>
    public required ImmutableArray<Version> SupportedGatewayFormats { get; init; }

    /// <summary>
    /// Gets or sets the list of source schema names included in this archive.
    /// These correspond to the distributed GraphQL services that comprise the composite schema.
    /// Each name must be a valid schema identifier according to the Fusion specification.
    /// </summary>
    public required ImmutableArray<string> SourceSchemas { get; init; }
}
