using Aspire.Hosting;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Provides extension methods for adding GraphQL orchestration to Aspire.
/// </summary>
public static class GraphQLOrchestratorExtensions
{
    /// <summary>
    /// Adds GraphQL schema composition orchestration to the distributed application. Every gateway
    /// composes the source schemas of the distributed application. Use
    /// <see cref="NitroExtensions.AddNitro"/> instead to also serve the source schemas that a
    /// fusion configuration in Nitro carries.
    /// </summary>
    /// <param name="builder">The distributed application builder</param>
    /// <returns>The distributed application builder for chaining</returns>
    public static IDistributedApplicationBuilder AddGraphQLOrchestrator(
        this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        SchemaCompositionRegistration.Ensure(builder);

        return builder;
    }
}
