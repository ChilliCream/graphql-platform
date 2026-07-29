namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Tells the schema composition whether the distributed application is connected to Nitro. It is
/// always registered, so the composition can be resolved with or without Nitro.
/// </summary>
/// <remarks>
/// <c>AddGraphQLOrchestrator</c> and <c>AddNitro</c> can be called in any order and both register
/// this instance, so <c>AddNitro</c> assigns the coordinator to the instance that is already
/// registered instead of registering a second one.
/// </remarks>
internal sealed class NitroCompositionOptions
{
    /// <summary>
    /// Gets or sets the coordinator that provides the fusion configurations, or <c>null</c> when
    /// the distributed application does not add Nitro.
    /// </summary>
    public NitroSeedCoordinator? Coordinator { get; set; }
}
