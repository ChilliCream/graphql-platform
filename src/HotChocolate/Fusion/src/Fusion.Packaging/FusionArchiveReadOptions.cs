namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Specifies the read options for a Fusion Archive.
/// </summary>
internal readonly record struct FusionArchiveReadOptions(
    int MaxAllowedSchemaSize,
    int MaxAllowedSettingsSize)
{
    /// <summary>
    /// Gets the default read options.
    /// </summary>
    public static FusionArchiveReadOptions Default { get; } = new(50_000_000, 512_000);
}
