using System;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Handlers;

namespace HotChocolate.Data.MongoDb
{
    public static class MongoDbProjectionProviderDescriptorExtensions
    {
        public static IProjectionProviderDescriptor AddMongoDbDefaults(
            this IProjectionProviderDescriptor descriptor) =>
            descriptor.RegisteMongoDbHandlers();

        public static IProjectionProviderDescriptor RegisteMongoDbHandlers(
            this IProjectionProviderDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.RegisterFieldHandler<MongoDbProjectionScalarHandler>();
            descriptor.RegisterFieldHandler<MongoDbProjectionFieldHandler>();
            descriptor.RegisterOptimizer<QueryablePagingProjectionOptimizer>();
            descriptor.RegisterOptimizer<IsProjectedProjectionOptimizer>();
            return descriptor;
        }
    }
}
