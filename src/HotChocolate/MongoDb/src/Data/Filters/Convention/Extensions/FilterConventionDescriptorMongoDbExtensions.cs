using HotChocolate.Data.Filters;

namespace HotChocolate.Data.MongoDb.Filters;

public static class FilterConventionDescriptorMongoDbExtensions
{
    /// <summary>
    /// Adds a <see cref="MongoDbFilterProvider"/> with default configuration
    /// </summary>
    /// <param name="descriptor">The descriptor where the provider is registered</param>
    /// <returns>The descriptor that was passed in as a parameter</returns>
    public static IFilterConventionDescriptor UseMongoDbProvider(
        this IFilterConventionDescriptor descriptor) =>
        descriptor.Provider(new MongoDbFilterProvider(x => x.AddDefaultMongoDbFieldHandlers()));

    /// <summary>
    /// Initializes the default configuration of the provider by registering handlers
    /// </summary>
    /// <param name="descriptor">The descriptor where the handlers are registered</param>
    /// <returns>The <paramref name="descriptor"/> that was passed in as a parameter</returns>
    public static IFilterProviderDescriptor<MongoDbFilterVisitorContext>
        AddDefaultMongoDbFieldHandlers(
            this IFilterProviderDescriptor<MongoDbFilterVisitorContext> descriptor)
    {
        descriptor.AddFieldHandler(MongoDbEqualsOperationHandler.Create);
        descriptor.AddFieldHandler(MongoDbNotEqualsOperationHandler.Create);

        descriptor.AddFieldHandler(MongoDbInOperationHandler.Create);
        descriptor.AddFieldHandler(MongoDbNotInOperationHandler.Create);

        descriptor.AddFieldHandler(MongoDbComparableGreaterThanHandler.Create);
        descriptor.AddFieldHandler(MongoDbComparableNotGreaterThanHandler.Create);
        descriptor.AddFieldHandler(MongoDbComparableGreaterThanOrEqualsHandler.Create);
        descriptor.AddFieldHandler(MongoDbComparableNotGreaterThanOrEqualsHandler.Create);
        descriptor.AddFieldHandler(MongoDbComparableLowerThanHandler.Create);
        descriptor.AddFieldHandler(MongoDbComparableNotLowerThanHandler.Create);
        descriptor.AddFieldHandler(MongoDbComparableLowerThanOrEqualsHandler.Create);
        descriptor.AddFieldHandler(MongoDbComparableNotLowerThanOrEqualsHandler.Create);

        descriptor.AddFieldHandler(MongoDbStringStartsWithHandler.Create);
        descriptor.AddFieldHandler(MongoDbStringNotStartsWithHandler.Create);
        descriptor.AddFieldHandler(MongoDbStringEndsWithHandler.Create);
        descriptor.AddFieldHandler(MongoDbStringNotEndsWithHandler.Create);
        descriptor.AddFieldHandler(MongoDbStringContainsHandler.Create);
        descriptor.AddFieldHandler(MongoDbStringNotContainsHandler.Create);

        descriptor.AddFieldHandler(MongoDbListAllOperationHandler.Create);
        descriptor.AddFieldHandler(MongoDbListAnyOperationHandler.Create);
        descriptor.AddFieldHandler(MongoDbListNoneOperationHandler.Create);
        descriptor.AddFieldHandler(MongoDbListSomeOperationHandler.Create);

        descriptor.AddFieldHandler(MongoDbDefaultFieldHandler.Create);

        return descriptor;
    }
}
