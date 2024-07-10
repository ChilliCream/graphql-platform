#if NET8_0_OR_GREATER
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.AspNetCore.ServerDefaults;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides <see cref="IHostApplicationBuilder"/> extension methods to configure a GraphQL server.
/// </summary>
public static class HotChocolateAspNetCoreHostingBuilderExtensions
{
    /// <summary>
    /// Adds the GraphQL server to the application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder"/>.
    /// </param>
    /// <param name="schemaName">
    /// The name of the GraphQL schema.
    /// </param>
    /// <param name="maxAllowedRequestSize">
    /// The max allowed GraphQL request size.
    /// </param>
    /// <param name="disableCostAnalyzer">
    /// Defines if the cost analyzer should be disabled.
    /// </param>
    /// <returns></returns>
    public static IRequestExecutorBuilder AddGraphQL(
        this IHostApplicationBuilder builder,
        string? schemaName = default,
        int maxAllowedRequestSize = MaxAllowedRequestSize,
        bool disableCostAnalyzer = false)
        => builder.Services.AddGraphQLServer(schemaName, maxAllowedRequestSize, disableCostAnalyzer);
}
#endif
