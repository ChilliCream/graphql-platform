using System;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Handlers;

namespace HotChocolate.Data.MongoDb
{
    public static class MongoDbProjectionProviderDescriptorExtensions
    {
        /// <summary>
        /// Initializes the default configuration for MongoDb on the convention by adding handlers
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IProjectionProviderDescriptor AddMongoDbDefaults(
            this IProjectionProviderDescriptor descriptor) =>
            descriptor.RegisterMongoDbHandlers();

        /// <summary>
        /// Registers projection handlers for mongodb
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        /// <exception cref="ArgumentNullException">
        /// Throws in case the argument <paramref name="descriptor"/> is null
        /// </exception>
        public static IProjectionProviderDescriptor RegisterMongoDbHandlers(
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
