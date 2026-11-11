using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Data.ElasticSearch;
using HotChocolate.Data.ElasticSearch.Filtering;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data;

/// <summary>
/// Common extensions of <see cref="ISchemaBuilder"/> for Elasticsearch.
/// </summary>
[Experimental(ElasticSearchExperiments.Provider)]
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

    /// <summary>
    /// Adds filtering over ES|QL to the schema, with the serializer options of the ES|QL client,
    /// which write the values that a filter on a list of objects compares.
    /// </summary>
    /// <param name="builder">The schema builder.</param>
    /// <param name="options">The serializer options of the ES|QL client.</param>
    /// <returns>The schema builder.</returns>
    public static ISchemaBuilder AddElasticSearchFiltering(this ISchemaBuilder builder, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return builder.AddFiltering(
            d => d.AddDefaultOperations()
                .BindDefaultTypes()
                .Provider(new ElasticSearchQueryableFilterProvider(options)));
    }
}
