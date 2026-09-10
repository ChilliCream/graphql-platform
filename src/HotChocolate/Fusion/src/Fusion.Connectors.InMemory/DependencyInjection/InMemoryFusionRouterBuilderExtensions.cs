using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Options;

namespace Microsoft.Extensions.DependencyInjection;

// The legacy extension surface holds the shared operations until the compatibility surface
// is removed. Calling it here forwards through the same configuration pipeline.
#pragma warning disable CS0618

/// <summary>
/// Extension methods for registering the in-memory connector on <see cref="IFusionRouterBuilder"/>.
/// </summary>
public static class InMemoryFusionRouterBuilderExtensions
{
    /// <summary>
    /// Registers a callback to modify the options used to compose in-memory source schemas.
    /// </summary>
    public static IFusionRouterBuilder ModifyInMemoryCompositionOptions(
        this IFusionRouterBuilder builder,
        Action<SchemaComposerOptions> configure)
    {
        InMemoryFusionGatewayBuilderExtensions.ModifyInMemoryCompositionOptions(builder, configure);
        return builder;
    }

    /// <summary>
    /// Adds an in-memory schema connector that executes operations directly in-process
    /// against the schema registered by the given <paramref name="schemaBuilder"/>.
    /// </summary>
    public static IFusionRouterBuilder AddInMemorySchema(
        this IFusionRouterBuilder builder,
        IRequestExecutorBuilder schemaBuilder)
    {
        InMemoryFusionGatewayBuilderExtensions.AddInMemorySchema(builder, schemaBuilder);
        return builder;
    }

    /// <summary>
    /// Adds an in-memory schema connector that executes operations directly in-process
    /// against the schema identified by <paramref name="schemaName"/>.
    /// </summary>
    public static IFusionRouterBuilder AddInMemorySchema(
        this IFusionRouterBuilder builder,
        string schemaName)
    {
        InMemoryFusionGatewayBuilderExtensions.AddInMemorySchema(builder, schemaName);
        return builder;
    }
}
