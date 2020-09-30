using System;
using HotChocolate.Data.Projections.Expressions.Handlers;

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

            descriptor.RegisterFieldHandler<QueryableProjectionLeafHandler>();
            return descriptor;
        }
    }
}
