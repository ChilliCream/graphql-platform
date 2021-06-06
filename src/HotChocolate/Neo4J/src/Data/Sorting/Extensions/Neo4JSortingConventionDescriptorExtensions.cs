using System;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.Neo4J.Sorting
{
    public static class Neo4JSortingConventionDescriptorExtensions
    {
        /// <summary>
        /// Adds a <see cref="Neo4JSortProvider"/> with default configuration
        /// </summary>
        /// <param name="descriptor">The descriptor where the provider is registered</param>
        /// <returns>The <paramref name="descriptor"/> that was passed in as a parameter</returns>
        public static ISortConventionDescriptor UseNeo4JProvider(
            this ISortConventionDescriptor descriptor) =>
            descriptor.Provider(new Neo4JSortProvider(x => x.AddDefaultFieldHandlers()));

        /// <summary>
        /// Initializes the default configuration of the provider by registering handlers
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/> that was passed in as a parameter</returns>
        public static ISortProviderDescriptor<Neo4JSortVisitorContext> AddDefaultFieldHandlers(
            this ISortProviderDescriptor<Neo4JSortVisitorContext> descriptor)
        {
            descriptor.AddOperationHandler<Neo4JAscendingSortOperationHandler>();
            descriptor.AddOperationHandler<Neo4JDescendingSortOperationHandler>();
            descriptor.AddFieldHandler<Neo4JDefaultSortFieldHandler>();
            return descriptor;
        }

        /// <summary>
        /// Initializes the default configuration for Neo4J on the convention by adding operations
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static ISortConventionDescriptor AddNeo4JDefaults(
            this ISortConventionDescriptor descriptor) =>
            descriptor.AddDefaultNeo4JOperations().BindDefaultNeo4JTypes().UseNeo4JProvider();

        /// <summary>
        /// Adds default operations for Neo4J to the descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        /// <exception cref="ArgumentNullException">
        /// Throws in case the argument <paramref name="descriptor"/> is null
        /// </exception>
        public static ISortConventionDescriptor AddDefaultNeo4JOperations(
            this ISortConventionDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Operation(DefaultSortOperations.Ascending).Name("ASC");
            descriptor.Operation(DefaultSortOperations.Descending).Name("DESC");

            return descriptor;
        }

        /// <summary>
        /// Binds common runtime types to the according <see cref="SortInputType"/> that are
        /// supported by Neo4J
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The descriptor that was passed in as a parameter</returns>
        /// <exception cref="ArgumentNullException">
        /// Throws in case the argument <paramref name="descriptor"/> is null
        /// </exception>
        public static ISortConventionDescriptor BindDefaultNeo4JTypes(
            this ISortConventionDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.BindRuntimeType<string, DefaultSortEnumType>();
            descriptor.DefaultBinding<DefaultSortEnumType>();

            return descriptor;
        }
    }
}
