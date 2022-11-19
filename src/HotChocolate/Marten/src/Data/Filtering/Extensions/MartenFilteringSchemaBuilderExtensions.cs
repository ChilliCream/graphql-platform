using HotChocolate.Data.Filters;
using HotChocolate.Data.Marten.Filtering;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Data;

/// <summary>
/// Contains internal helper methods to add filtering to the schema builder.
/// </summary>
public static class MartenFilteringSchemaBuilderExtensions
{
    /// <summary>
    /// Adds the MartenDB filter support to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL schema builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL schema builder for configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddMartenFiltering(
        this ISchemaBuilder builder)
        => builder
            .AddFiltering(d =>
                d.AddDefaultOperations()
                    .BindDefaultTypes()
                    .UseMartenQueryableFilterProvider());

    private static void UseMartenQueryableFilterProvider(
        this IFilterConventionDescriptor descriptor)
        => descriptor.Provider<MartenQueryableFilterProvider>();
}
