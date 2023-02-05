using HotChocolate.Data.Raven.Sorting;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data;

public static class RavenSortingSchemaBuilderExtensions
{
    public static ISchemaBuilder AddRavenSorting(this ISchemaBuilder schemaBuilder)
    {
        return schemaBuilder.AddSorting(x => x
            .AddDefaultOperations()
            .BindDefaultTypes()
            .UseRavenQueryableSortProvider());
    }

    private static void UseRavenQueryableSortProvider(this ISortConventionDescriptor descriptor)
    {
        descriptor.Provider(new RavenQueryableSortProvider(x => x.AddDefaultFieldHandlers()));
    }
}
