namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Tells the schema composition whether the distributed application is connected to Nitro. It is
/// always registered, so the composition can be resolved with or without Nitro.
/// </summary>
internal sealed class NitroCompositionOptions
{
    /// <summary>
    /// Gets or sets the coordinator that provides the fusion configurations, or <c>null</c> when
    /// the distributed application does not add Nitro.
    /// </summary>
    public NitroSeedCoordinator? Coordinator { get; set; }
}
