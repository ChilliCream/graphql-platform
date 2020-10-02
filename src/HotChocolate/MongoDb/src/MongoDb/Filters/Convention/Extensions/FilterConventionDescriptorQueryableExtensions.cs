using HotChocolate.Data.Filters;
using HotChocolate.MongoDb.Filters.Expressions;

namespace HotChocolate.MongoDb.Data.Filters
{
    public static class FilterConventionDescriptorMongoDbExtensions
    {
        public static IFilterConventionDescriptor UseMongoDbProvider(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.Provider(new MongoFilterProvider(x => x.AddDefaultMongoHandler()));

        public static IFilterProviderDescriptor<MongoFilterVisitorContext> AddDefaultMongoHandler(
            this IFilterProviderDescriptor<MongoFilterVisitorContext> descriptor)
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

            descriptor.AddFieldHandler<MongoListAllOperationHandler>();
            descriptor.AddFieldHandler<MongoListAnyOperationHandler>();
            descriptor.AddFieldHandler<MongoListNoneOperationHandler>();
            descriptor.AddFieldHandler<MongoListSomeOperationHandler>();

            descriptor.AddFieldHandler<MongoDefaultFieldHandler>();

            return descriptor;
        }
    }
}
