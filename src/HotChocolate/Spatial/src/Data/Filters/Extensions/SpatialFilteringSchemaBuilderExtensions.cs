using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Spatial;

namespace HotChocolate
{
    public static class SpatialFilteringSchemaBuilderExtensions
    {
        public static ISchemaBuilder AddSpatialFiltering(this ISchemaBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddConvention<IFilterConvention>(
                new FilterConventionExtension(x => x.AddSpatialDefaults()));
        }
    }
}
