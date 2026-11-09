namespace HotChocolate.Data;

/// <summary>
/// Common extensions of <see cref="ISchemaBuilder"/> for Elasticsearch.
/// </summary>
public static class ElasticSearchSortingSchemaBuilderExtensions
{
    /// <summary>
    /// Adds sorting over ES|QL to the schema.
    /// </summary>
    /// <param name="builder">The schema builder.</param>
    /// <returns>The schema builder.</returns>
    public static ISchemaBuilder AddElasticSearchSorting(this ISchemaBuilder builder)
        => builder.AddSorting();
}
