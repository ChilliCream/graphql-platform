using System;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Data.Projections.Handlers;

namespace HotChocolate.Data.Projections
{
    public static class ProjectionConventionDescriptorExtensions
    {
        public static IProjectionConventionDescriptor AddDefaults(
            this IProjectionConventionDescriptor descriptor) =>
            descriptor.RegisterQueryableHandler();

        public static IProjectionConventionDescriptor RegisterQueryableHandler(
            this IProjectionConventionDescriptor descriptor)
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

            descriptor.RegisterOptimizer<QueryableFilterProjectionOptimizer>();
            descriptor.RegisterOptimizer<QueryableSortProjectionOptimizer>();

            return descriptor;
        }
    }
}
