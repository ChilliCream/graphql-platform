namespace HotChocolate.Fusion.SourceSchema.Packaging;

/// <summary>
/// Specifies the read options for a Fusion source schema archive.
/// </summary>
internal readonly record struct FusionSourceSchemaArchiveReadOptions(
    int MaxAllowedSchemaSize,
    int MaxAllowedSettingsSize)
{
    /// <summary>
    /// Gets the default read options.
    /// </summary>
    public static FusionSourceSchemaArchiveReadOptions Default { get; }
        = new(50_000_000, 512_000);
}
