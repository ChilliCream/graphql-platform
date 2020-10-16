using HotChocolate.Data.Sorting;
using HotChocolate.MongoDb.Sorting.Convention.Extensions.Handlers;
using HotChocolate.MongoDb.Sorting.Handlers;

namespace HotChocolate.MongoDb.Sorting.Convention.Extensions
{
    public static class SortConventionDescriptorMongoExtensions
    {
        public static ISortConventionDescriptor UseMongoDbProvider(
            this ISortConventionDescriptor descriptor) =>
            descriptor.Provider(new MongoDbSortProvider(x => x.AddDefaultFieldHandlers()));

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
