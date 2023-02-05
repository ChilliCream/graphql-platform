using HotChocolate.Data.Filters;
using HotChocolate.Data.Raven.Filtering;

namespace HotChocolate.Data;

public static class RavenFilteringSchemaBuilderExtensions
{
    public static ISchemaBuilder AddRavenFiltering(this ISchemaBuilder schemaBuilder)
        => schemaBuilder
            .AddFiltering(
                t => t.ConfigureFilterType<IListFilterInputType>(
                        x => x.Operation(DefaultFilterOperations.All).Ignore())
                    .AddDefaultOperations()
                    .BindDefaultTypes()
                    .UseRavenQueryableFilterProvider());

    private static void UseRavenQueryableFilterProvider(this IFilterConventionDescriptor descriptor)
        => descriptor.Provider<RavenQueryableFilterProvider>();
}
