using Aspire.Hosting;
using Aspire.Hosting.Lifecycle;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Provides extension methods for adding GraphQL orchestration to Aspire.
/// </summary>
public static class GraphQLOrchestratorExtensions
{
    /// <summary>
    /// Adds GraphQL schema composition orchestration to the distributed application.
    /// </summary>
    /// <param name="builder">The distributed application builder</param>
    /// <returns>The distributed application builder for chaining</returns>
    public static IDistributedApplicationBuilder AddGraphQLOrchestrator(
        this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddLifecycleHook<SchemaCompositionHook>();
        return builder;
    }
}
