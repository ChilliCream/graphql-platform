namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Configures how Fusion Aspire follows changes to a Nitro stage during an AppHost run.
/// </summary>
public sealed class NitroSeedUpdateOptions
{
    /// <summary>
    /// Gets or sets whether Fusion Aspire subscribes to stage changes and downloads newer Fusion
    /// configurations. Enabling this option adds background subscription, query, and download
    /// traffic to the configured Nitro API. The default is <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether a newly downloaded Fusion configuration is immediately applied. When
    /// disabled, new configurations are staged and applied by the next local recomposition. The
    /// default is <see langword="true"/>.
    /// </summary>
    public bool AutoUpdate { get; set; } = true;
}
