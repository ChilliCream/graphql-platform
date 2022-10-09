using HotChocolate.Data.Marten.Sorting;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data;

public static class MartenSortingSchemaBuilderExtensions
{
    public static ISchemaBuilder AddMartenSorting(this ISchemaBuilder schemaBuilder)
    {
        return schemaBuilder.AddSorting(x => x
            .AddDefaultOperations()
            .BindDefaultTypes()
            .UseMartenQueryableSortProvider());
    }

    private static void UseMartenQueryableSortProvider(this ISortConventionDescriptor descriptor)
    {
        descriptor.Provider(new MartenQueryableSortProvider(x => x.AddDefaultFieldHandlers()));
    }
}
