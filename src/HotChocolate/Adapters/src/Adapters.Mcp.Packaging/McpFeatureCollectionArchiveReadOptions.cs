namespace HotChocolate.Adapters.Mcp.Packaging;

/// <summary>
/// Specifies the read options for an MCP Feature Collection archive.
/// </summary>
internal readonly record struct McpFeatureCollectionArchiveReadOptions(
    int MaxAllowedDocumentSize,
    int MaxAllowedSettingsSize,
    int MaxAllowedOpenAiComponentSize)
{
    /// <summary>
    /// Gets the default read options.
    /// </summary>
    public static McpFeatureCollectionArchiveReadOptions Default { get; }
        = new(50_000_000, 512_000, 50_000_000);
}
