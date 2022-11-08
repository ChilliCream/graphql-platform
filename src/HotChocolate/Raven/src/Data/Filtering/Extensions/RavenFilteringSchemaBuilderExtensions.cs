using HotChocolate.Data.Filters;
using HotChocolate.Data.Raven.Filtering;

namespace HotChocolate.Data;

public static class RavenFilteringSchemaBuilderExtensions
{
    public static ISchemaBuilder AddRavenFiltering(this ISchemaBuilder schemaBuilder)
    {
        return schemaBuilder.AddFiltering(x => x
            .AddDefaultOperations()
            .BindDefaultTypes()
            .UseRavenQueryableFilterProvider());
    }

    private static void UseRavenQueryableFilterProvider(
        this IFilterConventionDescriptor descriptor)
    {
        descriptor.Provider(new RavenQueryableFilterProvider(x =>
        {
            x.AddFieldHandler<RavenQueryableComparableInHandler>();
            x.AddFieldHandler<RavenQueryableComparableNotInHandler>();
            x.AddFieldHandler<RavenQueryableEnumInHandler>();
            x.AddFieldHandler<RavenQueryableEnumNotInHandler>();
            x.AddFieldHandler<RavenQueryableStringInHandler>();
            x.AddFieldHandler<RavenQueryableStringNotInHandler>();
            x.AddDefaultFieldHandlers();
        }));
    }
}
