using HotChocolate.Data.Marten.Sorting;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data;

/// <summary>
/// Contains internal helper methods to add filtering to the schema builder.
/// </summary>
public static class MartenSortingSchemaBuilderExtensions
{
    /// <summary>
    /// Adds the MartenDB sorting support to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL schema builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL schema builder for configuration chaining.
    /// </returns>
    public static ISchemaBuilder AddMartenSorting(this ISchemaBuilder builder)
        => builder
            .AddSorting(x =>
                x.AddDefaultOperations()
                    .BindDefaultTypes()
                    .UseMartenQueryableSortProvider());

    private static void UseMartenQueryableSortProvider(this ISortConventionDescriptor descriptor)
        => descriptor.Provider(new MartenQueryableSortProvider(x => x.AddDefaultFieldHandlers()));
}
