using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;

namespace HotChocolate.Data;

public static class ProjectionConventionDescriptorExtensions
{
    public static IProjectionConventionDescriptor AddDefaults(this IProjectionConventionDescriptor descriptor)
    {
        return descriptor.Provider(new QueryableProjectionProvider
        (
            x => x.AddDefaults
                <
                    QueryableProjectionScalarHandler<QueryableProjectionContext>,
                    QueryableProjectionListHandler<QueryableProjectionContext>,
                    QueryableProjectionObjectHandler<QueryableProjectionContext>
                >()
        ));
    }
}
