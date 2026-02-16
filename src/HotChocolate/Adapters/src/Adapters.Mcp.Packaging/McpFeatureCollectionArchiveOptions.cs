namespace HotChocolate.Adapters.Mcp.Packaging;

/// <summary>
/// Specifies the options for an MCP Feature Collection archive.
/// </summary>
public struct McpFeatureCollectionArchiveOptions
{
    /// <summary>
    /// Gets or sets the maximum allowed size of a document in the archive.
    /// </summary>
    public int? MaxAllowedDocumentSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed size of the settings in the archive.
    /// </summary>
    public int? MaxAllowedSettingsSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed size of the view (HTML) in the archive.
    /// </summary>
    public int? MaxAllowedViewSize { get; set; }
}
