using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions.Handlers;

namespace HotChocolate.Data.Extensions;

internal static class EntityFrameworkProjectionConventionDescriptorExtensions
{
    public static IProjectionConventionDescriptor AddEntityFrameworkDefaults(this IProjectionConventionDescriptor descriptor)
    {
        return descriptor.Provider(new EntityFrameworkQueryableProjectionProvider
        (
            x => x.AddDefaults
                <
                    QueryableProjectionScalarHandler<EntityFrameworkQueryableProjectionContext>,
                    QueryableProjectionListHandler<EntityFrameworkQueryableProjectionContext>,
                    EntityFrameworkQueryableProjectionObjectHandler
                >()
        ));
    }
}
