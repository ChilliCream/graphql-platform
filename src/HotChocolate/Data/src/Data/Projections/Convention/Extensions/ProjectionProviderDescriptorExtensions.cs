using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Data.Projections.Handlers;

namespace HotChocolate.Data;

public static class ProjectionProviderDescriptorExtensions
{
    public static IProjectionProviderDescriptor AddDefaults(
        this IProjectionProviderDescriptor descriptor) =>
        descriptor.RegisterQueryableHandler();

    public static IProjectionProviderDescriptor RegisterQueryableHandler(
        this IProjectionProviderDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.RegisterFieldHandler<QueryableProjectionScalarHandler>();
        descriptor.RegisterFieldHandler<QueryableProjectionListHandler>();
        descriptor.RegisterFieldHandler<QueryableProjectionFieldHandler>();

        descriptor.RegisterFieldInterceptor<QueryableFilterInterceptor>();
        descriptor.RegisterFieldInterceptor<QueryableSortInterceptor>();
        descriptor.RegisterFieldInterceptor<QueryableFirstOrDefaultInterceptor>();
        descriptor.RegisterFieldInterceptor<QueryableSingleOrDefaultInterceptor>();

        descriptor.RegisterOptimizer<IsProjectedProjectionOptimizer>();
        descriptor.RegisterOptimizer<QueryablePagingProjectionOptimizer>();
        descriptor.RegisterOptimizer<QueryableFilterProjectionOptimizer>();
        descriptor.RegisterOptimizer<QueryableSortProjectionOptimizer>();

        return descriptor;
    }
}
