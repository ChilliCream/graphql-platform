using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Spatial;

namespace HotChocolate;

public static class SpatialProjectionsSchemaBuilderExtensions
{
    public static ISchemaBuilder AddSpatialProjections(this ISchemaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddConvention<IProjectionConvention>(
            new ProjectionConventionExtension(
                x => x.AddProviderExtension(
                    new ProjectionProviderExtension(y => y.AddSpatialHandlers()))));
    }
}
