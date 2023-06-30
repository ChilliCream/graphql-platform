namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Represents the context data available to resolve the fusion gateway configuration.
/// </summary>
public readonly struct GatewayConfigurationContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="GatewayConfigurationContext"/>.
    /// </summary>
    /// <param name="services">
    /// The service provider.
    /// </param>
    public GatewayConfigurationContext(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider Services { get; }
}
