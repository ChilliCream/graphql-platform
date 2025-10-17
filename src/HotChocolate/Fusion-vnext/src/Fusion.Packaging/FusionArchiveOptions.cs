namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Specifies the options for a Fusion Archive.
/// </summary>
public struct FusionArchiveOptions
{
    /// <summary>
    /// Gets or sets the maximum allowed size of a schema in the archive.
    /// </summary>
    public int? MaxAllowedSchemaSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed size of the settings in the archive.
    /// </summary>
    public int? MaxAllowedSettingsSize { get; set; }
}
