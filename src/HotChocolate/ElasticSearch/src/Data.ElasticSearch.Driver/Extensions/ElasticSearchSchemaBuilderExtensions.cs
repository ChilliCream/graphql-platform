using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.ElasticSearch.Sorting.Convention;

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
        builder.AddFiltering(new ElasticSearchFilterConvention(), name);

    public static ISchemaBuilder AddElasticSearchSorting(
        this ISchemaBuilder builder,
        string? name = null) =>
        builder.AddSorting(x => x.AddElasticSearchDefaults(), name);
}
