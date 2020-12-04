using System;
using HotChocolate.Data;
using HotChocolate.Data.Filters;

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
