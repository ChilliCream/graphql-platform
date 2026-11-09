using System.Text.Json;
using HotChocolate.Data;
using HotChocolate.Data.ElasticSearch.Filtering;
using HotChocolate.Data.Filters;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Common extensions of <see cref="IRequestExecutorBuilder"/> for Elasticsearch.
/// </summary>
public static class ElasticSearchFilteringRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds filtering over ES|QL to the schema.
    /// </summary>
    /// <param name="builder">The request executor builder.</param>
    /// <param name="namingPolicy">
    /// The naming policy of the document fields, the same one the ES|QL client uses.
    /// </param>
    /// <returns>The request executor builder.</returns>
    public static IRequestExecutorBuilder AddElasticSearchFiltering(
        this IRequestExecutorBuilder builder,
        JsonNamingPolicy? namingPolicy)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddFiltering(
            c => c.AddDefaults().Provider(new ElasticSearchQueryableFilterProvider(namingPolicy)));
    }

    /// <summary>
    /// Adds filtering over ES|QL to the schema, for documents with camel case field names.
    /// </summary>
    /// <param name="builder">The request executor builder.</param>
    /// <returns>The request executor builder.</returns>
    public static IRequestExecutorBuilder AddElasticSearchFiltering(this IRequestExecutorBuilder builder)
        => builder.AddElasticSearchFiltering(JsonNamingPolicy.CamelCase);
}
