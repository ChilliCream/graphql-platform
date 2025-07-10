using HotChocolate.Execution.Configuration;
using HotChocolate.Features;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides global configuration methods for query conventions to the
/// <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class QueryRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Enables query conventions which will simplify creating GraphQL queries.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder
    /// </param>
    /// <returns>
    /// The request executor builder
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is null.
    /// </exception>
    public static IRequestExecutorBuilder AddQueryConventions(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ConfigureSchema(c => c.Features.GetOrSet<ErrorSchemaFeature>())
            .AddFieldResultTypeDiscovery()
            .TryAddTypeInterceptor<QueryConventionTypeInterceptor>();

        return builder;
    }
}
