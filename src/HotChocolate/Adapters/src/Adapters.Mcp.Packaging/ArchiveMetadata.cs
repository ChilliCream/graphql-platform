using System.Collections.Immutable;

namespace HotChocolate.Adapters.Mcp.Packaging;

/// <summary>
/// Contains metadata about an MCP Feature Collection archive.
/// </summary>
public record ArchiveMetadata
{
    /// <summary>
    /// Gets or sets the version of the MCP Feature Collection archive format specification.
    /// Used to ensure compatibility between different versions of tooling.
    /// </summary>
    public Version FormatVersion { get; init; } = new("1.0.0");

    /// <summary>
    /// Gets or sets the names of prompts contained in this archive.
    /// </summary>
    public ImmutableArray<string> Prompts { get; init; } = [];

    /// <summary>
    /// Gets or sets the names of tools contained in this archive.
    /// </summary>
    public ImmutableArray<string> Tools { get; init; } = [];
}
