using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Data;
using HotChocolate.Data.ElasticSearch;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Common extensions of <see cref="IRequestExecutorBuilder"/> for Elasticsearch.
/// </summary>
[Experimental(ElasticSearchExperiments.Provider)]
public static class ElasticSearchFilteringRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds filtering over ES|QL to the schema, for documents with camel case field names.
    /// </summary>
    /// <param name="builder">The request executor builder.</param>
    /// <returns>The request executor builder.</returns>
    public static IRequestExecutorBuilder AddElasticSearchFiltering(this IRequestExecutorBuilder builder)
        => builder.ConfigureSchema(x => x.AddElasticSearchFiltering());

    /// <summary>
    /// Adds filtering over ES|QL to the schema.
    /// </summary>
    /// <param name="builder">The request executor builder.</param>
    /// <param name="namingPolicy">The naming policy of the document fields.</param>
    /// <returns>The request executor builder.</returns>
    public static IRequestExecutorBuilder AddElasticSearchFiltering(
        this IRequestExecutorBuilder builder,
        JsonNamingPolicy? namingPolicy)
        => builder.ConfigureSchema(x => x.AddElasticSearchFiltering(namingPolicy));

    /// <summary>
    /// Adds filtering over ES|QL to the schema, with the serializer options of the ES|QL client,
    /// which give the field names and the values that a filter on a list of objects compares.
    /// </summary>
    /// <param name="builder">The request executor builder.</param>
    /// <param name="options">The serializer options of the ES|QL client.</param>
    /// <returns>The request executor builder.</returns>
    public static IRequestExecutorBuilder AddElasticSearchFiltering(
        this IRequestExecutorBuilder builder,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return builder.ConfigureSchema(x => x.AddElasticSearchFiltering(options));
    }
}
