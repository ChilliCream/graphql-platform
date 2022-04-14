using HotChocolate.Data.ElasticSearch.Filters;

namespace HotChocolate.Data.ElasticSearch;

/// <summary>
/// Provides mongo extensions for the <see cref="ISchemaBuilder"/>.
/// </summary>
public static class ElasticSearchSchemaBuilderExtensions
{
    /// <summary>
    /// Adds filtering support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="name"></param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/>.
    /// </returns>
    public static ISchemaBuilder AddElasticSearchFiltering(
        this ISchemaBuilder builder,
        string? name = null) =>
        builder.AddFiltering(x => x.AddElasticSearchDefaults(), name);
}
