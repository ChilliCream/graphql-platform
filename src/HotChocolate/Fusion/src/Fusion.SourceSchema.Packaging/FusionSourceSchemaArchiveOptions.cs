namespace HotChocolate.Fusion.SourceSchema.Packaging;

/// <summary>
/// Specifies the options for a Fusion source schema archive.
/// </summary>
public struct FusionSourceSchemaArchiveOptions
{
    /// <summary>
    /// Gets or sets the maximum allowed size of the GraphQL schema in the archive.
    /// </summary>
    public int? MaxAllowedSchemaSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed size of the settings in the archive.
    /// </summary>
    public int? MaxAllowedSettingsSize { get; set; }
}
