using HotChocolate.Data;
using HotChocolate.Data.Filters;

namespace HotChocolate;

public static class SpatialFilteringSchemaBuilderExtensions
{
    public static ISchemaBuilder AddSpatialFiltering(this ISchemaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddConvention<IFilterConvention>(
            new FilterConventionExtension(x => x.AddSpatialDefaults()));
    }
}
