using System.Collections.Immutable;

namespace HotChocolate.Adapters.OpenApi.Packaging;

/// <summary>
/// Contains metadata about an OpenAPI collection archive.
/// </summary>
public record ArchiveMetadata
{
    /// <summary>
    /// Gets or sets the version of the OpenAPI collection archive format specification.
    /// Used to ensure compatibility between different versions of tooling.
    /// </summary>
    public Version FormatVersion { get; init; } = new("1.0.0");

    /// <summary>
    /// Gets or sets the names of endpoints contained in this archive.
    /// </summary>
    public ImmutableArray<OpenApiEndpointKey> Endpoints { get; init; } = [];

    /// <summary>
    /// Gets or sets the names of models contained in this archive.
    /// </summary>
    public ImmutableArray<string> Models { get; init; } = [];
}
