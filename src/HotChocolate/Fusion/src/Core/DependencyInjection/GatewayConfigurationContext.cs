using HotChocolate.Execution.Configuration;

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
    /// <param name="typeModules">
    ///
    /// </param>
    public GatewayConfigurationContext(IServiceProvider services, IReadOnlyList<ITypeModule> typeModules)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        TypeModules = typeModules;
    }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider Services { get; }


    /// <summary>
    /// Get Gateway Type Modules.
    /// </summary>
    public IReadOnlyList<ITypeModule> TypeModules { get; }
}
