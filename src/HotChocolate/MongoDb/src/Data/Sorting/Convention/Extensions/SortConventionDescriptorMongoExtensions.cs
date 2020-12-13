using HotChocolate.Data.Sorting;
using HotChocolate.Data.MongoDb.Sorting.Convention.Extensions.Handlers;
using HotChocolate.Data.MongoDb.Sorting.Handlers;

namespace HotChocolate.Data.MongoDb.Sorting.Convention.Extensions
{
    public static class SortConventionDescriptorMongoExtensions
    {
        /// <summary>
        /// Adds a <see cref="MongoDbSortProvider"/> with default configuration
        /// </summary>
        /// <param name="descriptor">The descriptor where the provider is registered</param>
        /// <returns>The <paramref name="descriptor"/> that was passed in as a parameter</returns>
        public static ISortConventionDescriptor UseMongoDbProvider(
            this ISortConventionDescriptor descriptor) =>
            descriptor.Provider(new MongoDbSortProvider(x => x.AddDefaultFieldHandlers()));

        /// <summary>
        /// Initializes the default configuration of the provider by registering handlers
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/> that was passed in as a parameter</returns>
        public static ISortProviderDescriptor<MongoDbSortVisitorContext> AddDefaultFieldHandlers(
            this ISortProviderDescriptor<MongoDbSortVisitorContext> descriptor)
        {
            descriptor.AddOperationHandler<MongoDbAscendingSortOperationHandler>();
            descriptor.AddOperationHandler<MongoDbDescendingSortOperationHandler>();
            descriptor.AddFieldHandler<MongoDbDefaultSortFieldHandler>();
            return descriptor;
        }
    }
}
