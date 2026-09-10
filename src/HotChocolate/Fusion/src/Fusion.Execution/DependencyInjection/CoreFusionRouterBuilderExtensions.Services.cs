using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Resolves an application service and makes it available as a singleton in the schema services.
    /// </summary>
    public static IFusionRouterBuilder AddApplicationService<TService>(
        this IFusionRouterBuilder builder)
        where TService : class
    {
        CoreFusionGatewayBuilderExtensions.AddApplicationService<TService>(builder);
        return builder;
    }

    /// <summary>
    /// Builds the service provider and resolves the request executor for the specified schema.
    /// </summary>
    public static ValueTask<IRequestExecutor> BuildRequestExecutorAsync(
        this IFusionRouterBuilder builder,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
        => CoreFusionGatewayBuilderExtensions.BuildRequestExecutorAsync(builder, schemaName, cancellationToken);
}
