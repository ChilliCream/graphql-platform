namespace HotChocolate.Adapters.OpenApi.Packaging;

/// <summary>
/// Specifies the options for an OpenAPI collection archive.
/// </summary>
public struct OpenApiCollectionArchiveOptions
{
    /// <summary>
    /// Gets or sets the maximum allowed size of a document in the archive.
    /// </summary>
    public int? MaxAllowedDocumentSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed size of the settings in the archive.
    /// </summary>
    public int? MaxAllowedSettingsSize { get; set; }
}
