using HotChocolate.Data.ElasticSearch.Pagination;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Common extensions of <see cref="IRequestExecutorBuilder"/> for Elasticsearch pagination.
/// </summary>
public static class ElasticSearchPagingRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds the offset paging provider for ES|QL queries as the default provider.
    /// </summary>
    /// <param name="builder">The request executor builder.</param>
    /// <returns>The request executor builder.</returns>
    public static IRequestExecutorBuilder AddElasticSearchPagingProviders(this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddOffsetPagingProvider<ElasticSearchOffsetPagingProvider>(defaultProvider: true);
    }
}
