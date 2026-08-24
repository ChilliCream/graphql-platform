namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The sidecar of a cached fusion configuration. The cache file name is a hash, so the metadata
/// repeats the identifying values in clear text and is cross-checked when the entry is read.
/// </summary>
internal sealed class NitroSeedMetadata
{
    /// <summary>
    /// Gets the normalized Nitro API base URL the fusion configuration was downloaded from.
    /// </summary>
    public string ApiUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the id of the api that carries the fusion configuration.
    /// </summary>
    public string ApiId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the stage the fusion configuration was downloaded for.
    /// </summary>
    public string Stage { get; init; } = string.Empty;

    /// <summary>
    /// Gets the point in time at which the fusion configuration was downloaded.
    /// </summary>
    public DateTimeOffset DownloadedAt { get; init; }

    /// <summary>
    /// Gets the gateway format version the fusion configuration was downloaded for. An entry
    /// that was downloaded for another format version is discarded.
    /// </summary>
    public string FusionVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets the marker of the cached file this metadata describes. An entry whose file does not
    /// match the marker is the result of an interrupted promotion and is discarded.
    /// </summary>
    public string ContentMarker { get; init; } = string.Empty;
}
