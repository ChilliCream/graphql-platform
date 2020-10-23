using System;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Data.Projections.Handlers;

namespace HotChocolate.Data.Projections
{
    public static class ProjectionProviderDescriptorExtensions
    {
        public static IProjectionProviderDescriptor AddDefaults(
            this IProjectionProviderDescriptor descriptor) =>
            descriptor.RegisterQueryableHandler();

        public static IProjectionProviderDescriptor RegisterQueryableHandler(
            this IProjectionProviderDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

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
}
