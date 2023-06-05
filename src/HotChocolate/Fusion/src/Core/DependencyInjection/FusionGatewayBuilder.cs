using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder for configuring a GraphQL gateway.
/// </summary>
public sealed class FusionGatewayBuilder
{
    /// <summary>
    /// Initializes a new instance of <see cref="FusionGatewayBuilder"/>.
    /// </summary>
    /// <param name="coreBuilder">
    /// The underlying request executor builder.
    /// </param>
    public FusionGatewayBuilder(IRequestExecutorBuilder coreBuilder)
    {
        CoreBuilder = coreBuilder;
    }

    /// <summary>
    /// Gets the name of the schema.
    /// </summary>
    public string Name => CoreBuilder.Name;

    /// <summary>
    /// Gets the application services.
    /// </summary>
    public IServiceCollection Services => CoreBuilder.Services;

    /// <summary>
    /// Gets the underlying request executor builder.
    /// </summary>
    public IRequestExecutorBuilder CoreBuilder { get; }
}
