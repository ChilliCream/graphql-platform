using System.Text.Json;
using HotChocolate.Data.ElasticSearch.Filtering;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data;

/// <summary>
/// Common extensions of <see cref="ISchemaBuilder"/> for Elasticsearch.
/// </summary>
public static class ElasticSearchFilteringSchemaBuilderExtensions
{
    /// <summary>
    /// Adds filtering over ES|QL to the schema, for documents with camel case field names.
    /// </summary>
    /// <param name="builder">The schema builder.</param>
    /// <returns>The schema builder.</returns>
    public static ISchemaBuilder AddElasticSearchFiltering(this ISchemaBuilder builder)
        => builder.AddElasticSearchFiltering(JsonNamingPolicy.CamelCase);

    /// <summary>
    /// Adds filtering over ES|QL to the schema.
    /// </summary>
    /// <param name="builder">The schema builder.</param>
    /// <param name="namingPolicy">The naming policy of the document fields.</param>
    /// <returns>The schema builder.</returns>
    public static ISchemaBuilder AddElasticSearchFiltering(this ISchemaBuilder builder, JsonNamingPolicy? namingPolicy)
        => builder.AddFiltering(
            d => d.AddDefaultOperations()
                .BindDefaultTypes()
                .Provider(new ElasticSearchQueryableFilterProvider(namingPolicy)));
}
