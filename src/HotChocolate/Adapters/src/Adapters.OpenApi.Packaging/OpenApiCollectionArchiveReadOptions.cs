namespace HotChocolate.Adapters.OpenApi.Packaging;

/// <summary>
/// Specifies the read options for an OpenAPI collection archive.
/// </summary>
internal readonly record struct OpenApiCollectionArchiveReadOptions(
    int MaxAllowedOperationSize,
    int MaxAllowedSettingsSize)
{
    /// <summary>
    /// Gets the default read options.
    /// </summary>
    public static OpenApiCollectionArchiveReadOptions Default { get; }
        = new(50_000_000, 512_000);
}
