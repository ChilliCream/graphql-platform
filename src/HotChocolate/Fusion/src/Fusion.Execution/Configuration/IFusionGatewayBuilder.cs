using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Represents a builder for a Fusion gateway.
/// </summary>
// TODO [17]: Remove the legacy builder interface.
[Obsolete("Use IFusionRouterBuilder instead.")]
public interface IFusionGatewayBuilder
{
    /// <summary>
    /// Gets the name of the schema.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the application services.
    /// </summary>
    IServiceCollection Services { get; }
}
