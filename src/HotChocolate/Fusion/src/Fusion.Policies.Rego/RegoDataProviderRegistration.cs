namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// Carries the information needed to materialize one registered <see cref="IRegoDataProvider"/>
/// once the schema services container is available. Collected by <see cref="RegoDataAggregator"/>
/// from every <c>AddRegoDataProvider</c> call made on a gateway.
/// </summary>
internal sealed class RegoDataProviderRegistration(
    string name,
    Func<IServiceProvider, IRegoDataProvider> factory,
    bool ownsInstance)
{
    /// <summary>
    /// Gets the name the provider is registered under. Must be unique across all providers
    /// registered on the same gateway.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the factory that produces the provider instance from the schema services container.
    /// Invoked exactly once, when the aggregator is constructed.
    /// </summary>
    public Func<IServiceProvider, IRegoDataProvider> Factory { get; } = factory;

    /// <summary>
    /// Gets a value indicating whether the aggregator disposes the produced instance. <c>false</c>
    /// for a caller-supplied instance or a container-managed singleton the container itself
    /// already disposes.
    /// </summary>
    public bool OwnsInstance { get; } = ownsInstance;
}
