using HotChocolate.Data.Filters;
using HotChocolate.Data.Marten.Filtering;

namespace HotChocolate.Data;

public static class MartenFilteringSchemaBuilderExtensions
{
    public static ISchemaBuilder AddMartenFiltering(this ISchemaBuilder schemaBuilder)
    {
        return schemaBuilder.AddFiltering(x => x
            .AddDefaultOperations()
            .BindDefaultTypes()
            .UseMartenQueryableFilterProvider());
    }

    private static void UseMartenQueryableFilterProvider(
        this IFilterConventionDescriptor descriptor)
    {
        descriptor.Provider(new MartenQueryableFilterProvider(x => x.AddDefaultFieldHandlers()));
    }
}
