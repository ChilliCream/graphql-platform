using System.Reflection.Metadata.Ecma335;
using HotChocolate;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Represents a builder for a Fusion gateway.
/// </summary>
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
