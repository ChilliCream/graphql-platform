using System;
using HotChocolate.Data.Projections.Spatial;
using HotChocolate.Data.Projections;

namespace HotChocolate
{
    public static class SpatialProjectionsSchemaBuilderExtensions
    {
        public static ISchemaBuilder AddSpatialProjections(this ISchemaBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddConvention<IProjectionConvention>(
                new ProjectionConventionExtension(
                    x => x.AddProviderExtension(
                        new ProjectionProviderExtension(y => y.AddSpatialHandlers()))));
        }
    }
}
