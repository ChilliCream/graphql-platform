using System;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Handlers;

namespace HotChocolate.Data.Neo4J.Projections
{
    public static class Neo4JProjectionProviderDescriptorExtensions
    {
        /// <summary>
        /// Initializes the default configuration for Neo4J on the convention by adding handlers
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IProjectionProviderDescriptor AddNeo4JDefaults(
            this IProjectionProviderDescriptor descriptor) =>
            descriptor.RegisterNeo4JHandlers();

        /// <summary>
        /// Registers projection handlers for Neo4J
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        /// <exception cref="ArgumentNullException">
        /// Throws in case the argument <paramref name="descriptor"/> is null
        /// </exception>
        public static IProjectionProviderDescriptor RegisterNeo4JHandlers(
            this IProjectionProviderDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.RegisterFieldHandler<Neo4JProjectionScalarHandler>();
            descriptor.RegisterFieldHandler<Neo4JProjectionFieldHandler>();
            descriptor.RegisterOptimizer<IsProjectedProjectionOptimizer>();
            return descriptor;
        }
    }
}
