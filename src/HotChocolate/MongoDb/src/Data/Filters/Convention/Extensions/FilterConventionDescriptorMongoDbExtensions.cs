using HotChocolate.Data.Filters;

namespace HotChocolate.Data.MongoDb.Filters
{
    public static class FilterConventionDescriptorMongoDbExtensions
    {
        public static IFilterConventionDescriptor UseMongoDbProvider(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.Provider(new MongoDbFilterProvider(x => x.AddDefaultMongoHandler()));

        public static IFilterProviderDescriptor<MongoDbFilterVisitorContext> AddDefaultMongoHandler(
            this IFilterProviderDescriptor<MongoDbFilterVisitorContext> descriptor)
        {
            descriptor.AddFieldHandler<MongoDbEqualsOperationHandler>();
            descriptor.AddFieldHandler<MongoDbNotEqualsOperationHandler>();

            descriptor.AddFieldHandler<MongoDbInOperationHandler>();
            descriptor.AddFieldHandler<MongoDbNotInOperationHandler>();

            descriptor.AddFieldHandler<MongoDbComparableGreaterThanHandler>();
            descriptor.AddFieldHandler<MongoDbComparableNotGreaterThanHandler>();
            descriptor.AddFieldHandler<MongoDbComparableGreaterThanOrEqualsHandler>();
            descriptor.AddFieldHandler<MongoDbComparableNotGreaterThanOrEqualsHandler>();
            descriptor.AddFieldHandler<MongoDbComparableLowerThanHandler>();
            descriptor.AddFieldHandler<MongoDbComparableNotLowerThanHandler>();
            descriptor.AddFieldHandler<MongoDbComparableLowerThanOrEqualsHandler>();
            descriptor.AddFieldHandler<MongoDbComparableNotLowerThanOrEqualsHandler>();

            descriptor.AddFieldHandler<MongoDbStringStartsWithHandler>();
            descriptor.AddFieldHandler<MongoDbStringNotStartsWithHandler>();
            descriptor.AddFieldHandler<MongoDbStringEndsWithHandler>();
            descriptor.AddFieldHandler<MongoDbStringNotEndsWithHandler>();
            descriptor.AddFieldHandler<MongoDbStringContainsHandler>();
            descriptor.AddFieldHandler<MongoDbStringNotContainsHandler>();

            descriptor.AddFieldHandler<MongoDbListAllOperationHandler>();
            descriptor.AddFieldHandler<MongoDbListAnyOperationHandler>();
            descriptor.AddFieldHandler<MongoDbListNoneOperationHandler>();
            descriptor.AddFieldHandler<MongoDbListSomeOperationHandler>();

            descriptor.AddFieldHandler<MongoDbDefaultFieldHandler>();

            return descriptor;
        }
    }
}
