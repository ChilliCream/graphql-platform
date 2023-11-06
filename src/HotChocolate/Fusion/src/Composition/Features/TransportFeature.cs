namespace HotChocolate.Fusion.Composition.Features;

/// <summary>
/// Specifies transport defaults for the fusion graph.
/// </summary>
public sealed class TransportFeature : IFusionFeature
{
    /// <summary>
    /// Gets or sets the default client name that is used
    /// when no explicit client name was specified for a subgraph.
    /// </summary>
    public string? DefaultClientName { get; set; } = "Fusion";
}
