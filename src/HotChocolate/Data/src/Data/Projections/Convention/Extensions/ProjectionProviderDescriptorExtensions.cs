using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Data.Projections.Handlers;
using HotChocolate.Data.Projections.Optimizers;

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

        descriptor.RegisterFieldHandler(QueryableProjectionScalarHandler.Create);
        descriptor.RegisterFieldHandler(QueryableProjectionListHandler.Create);
        descriptor.RegisterFieldHandler(QueryableProjectionFieldHandler.Create);

        descriptor.RegisterFieldInterceptor(QueryableFilterInterceptor.Create);
        descriptor.RegisterFieldInterceptor(QueryableSortInterceptor.Create);
        descriptor.RegisterFieldInterceptor(QueryableFirstOrDefaultInterceptor.Create);
        descriptor.RegisterFieldInterceptor(QueryableSingleOrDefaultInterceptor.Create);

        descriptor.RegisterOptimizer(IsProjectedProjectionOptimizer.Create);
        descriptor.RegisterOptimizer(QueryablePagingProjectionOptimizer.Create);
        descriptor.RegisterOptimizer(QueryableFilterProjectionOptimizer.Create);
        descriptor.RegisterOptimizer(QueryableSortProjectionOptimizer.Create);

        return descriptor;
    }
}
