using Aspire.Hosting;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Configures how Fusion Aspire follows changes to a Nitro stage during an AppHost run.
/// </summary>
[AspireDto]
public sealed class NitroSeedUpdateOptions
{
    /// <summary>
    /// Gets whether Fusion Aspire subscribes to stage changes and downloads newer Fusion
    /// configurations. The default is <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets whether a newly downloaded Fusion configuration is immediately applied. When disabled,
    /// new configurations are staged and applied by the next local recomposition. The default is
    /// <see langword="true"/>.
    /// </summary>
    public bool AutoUpdate { get; init; } = true;
}
