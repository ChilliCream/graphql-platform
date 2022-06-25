using System;
using System.Linq;

namespace HotChocolate.Data;

/// <summary>
/// Provides filtering extensions for the <see cref="ISchemaBuilder"/> specially for
/// <see cref="IQueryable{T}"/>
/// </summary>
public static class QueryableFilterSchemaBuilderExtensions
{
    /// <summary>
    /// Adds filtering support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The filter convention name.
    /// </param>
    /// <param name="compatabilityMode">Uses the old behaviour of naming the filters</param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/>.
    /// </returns>
    public static ISchemaBuilder AddQueryableFiltering(
        this ISchemaBuilder builder,
        string? name = null,
        bool compatabilityMode = false) =>
        builder.AddFiltering(name, compatabilityMode);

    /// <summary>
    /// Adds filtering support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// Configures the convention.
    /// </param>
    /// <param name="name">
    /// The filter convention name.
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/>.
    /// </returns>
    public static ISchemaBuilder AddQueryableFiltering(
        this ISchemaBuilder builder,
        Action<IQueryableFilterConventionDescriptor> configure,
        string? name = null) =>
        builder.AddFiltering(d => configure(new QueryableFilterConventionDescriptor(d)), name);
}
