using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.SchemaBuilding;

/// <summary>
/// The GraphQL Gateway configuration builder.
/// </summary>
public interface IGatewayBuilder
{
    /// <summary>
    /// Gets the name of the schema.
    /// </summary>
    NameString Name { get; }

    /// <summary>
    /// Gets the application services.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the underlying GraphQL configuration builder.
    /// </summary>
    IRequestExecutorBuilder Builder { get; }
}
