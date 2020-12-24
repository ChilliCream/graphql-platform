using System;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data
{
    public static class MongoSortingConventionDescriptorExtensions
    {
        /// <summary>
        /// Initializes the default configuration for MongoDb on the convention by adding operations
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static ISortConventionDescriptor AddMongoDbDefaults(
            this ISortConventionDescriptor descriptor) =>
            descriptor.AddDefaultMongoDbOperations().BindDefaultMongoDbTypes().UseMongoDbProvider();

        /// <summary>
        /// Adds default operations for MongoDb to the descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        /// <exception cref="ArgumentNullException">
        /// Throws in case the argument <paramref name="descriptor"/> is null
        /// </exception>
        public static ISortConventionDescriptor AddDefaultMongoDbOperations(
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
        /// supported by MongoDb
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The descriptor that was passed in as a parameter</returns>
        /// <exception cref="ArgumentNullException">
        /// Throws in case the argument <paramref name="descriptor"/> is null
        /// </exception>
        public static ISortConventionDescriptor BindDefaultMongoDbTypes(
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
