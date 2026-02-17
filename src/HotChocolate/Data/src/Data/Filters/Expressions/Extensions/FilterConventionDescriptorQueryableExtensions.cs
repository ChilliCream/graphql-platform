using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data;

public static class FilterConventionDescriptorQueryableExtensions
{
    /// <summary>
    /// Adds a <see cref="QueryableFilterProvider"/> with default configuration
    /// </summary>
    /// <param name="descriptor">The descriptor where the provider is registered</param>
    /// <returns>The descriptor that was passed in as a parameter</returns>
    public static IFilterConventionDescriptor UseQueryableProvider(
        this IFilterConventionDescriptor descriptor) =>
        descriptor.Provider(new QueryableFilterProvider(x => x.AddDefaultFieldHandlers()));

    /// <summary>
    /// Initializes the default configuration of the provider by registering handlers
    /// </summary>
    /// <param name="descriptor">The descriptor where the handlers are registered</param>
    /// <returns>The descriptor that was passed in as a parameter</returns>
    public static IFilterProviderDescriptor<QueryableFilterContext> AddDefaultFieldHandlers(
        this IFilterProviderDescriptor<QueryableFilterContext> descriptor)
    {
        descriptor.AddFieldHandler(QueryableBooleanEqualsHandler.Create);
        descriptor.AddFieldHandler(QueryableBooleanNotEqualsHandler.Create);

        descriptor.AddFieldHandler(QueryableComparableEqualsHandler.Create);
        descriptor.AddFieldHandler(QueryableComparableNotEqualsHandler.Create);
        descriptor.AddFieldHandler(QueryableComparableInHandler.Create);
        descriptor.AddFieldHandler(QueryableComparableNotInHandler.Create);
        descriptor.AddFieldHandler(QueryableComparableGreaterThanHandler.Create);
        descriptor.AddFieldHandler(QueryableComparableNotGreaterThanHandler.Create);
        descriptor.AddFieldHandler(QueryableComparableGreaterThanOrEqualsHandler.Create);
        descriptor.AddFieldHandler(QueryableComparableNotGreaterThanOrEqualsHandler.Create);
        descriptor.AddFieldHandler(QueryableComparableLowerThanHandler.Create);
        descriptor.AddFieldHandler(QueryableComparableNotLowerThanHandler.Create);
        descriptor.AddFieldHandler(QueryableComparableLowerThanOrEqualsHandler.Create);
        descriptor.AddFieldHandler(QueryableComparableNotLowerThanOrEqualsHandler.Create);

        descriptor.AddFieldHandler(QueryableStringEqualsHandler.Create);
        descriptor.AddFieldHandler(QueryableStringNotEqualsHandler.Create);
        descriptor.AddFieldHandler(QueryableStringStartsWithHandler.Create);
        descriptor.AddFieldHandler(QueryableStringNotStartsWithHandler.Create);
        descriptor.AddFieldHandler(QueryableStringEndsWithHandler.Create);
        descriptor.AddFieldHandler(QueryableStringNotEndsWithHandler.Create);
        descriptor.AddFieldHandler(QueryableStringInHandler.Create);
        descriptor.AddFieldHandler(QueryableStringNotInHandler.Create);
        descriptor.AddFieldHandler(QueryableStringContainsHandler.Create);
        descriptor.AddFieldHandler(QueryableStringNotContainsHandler.Create);

        descriptor.AddFieldHandler(QueryableEnumEqualsHandler.Create);
        descriptor.AddFieldHandler(QueryableEnumNotEqualsHandler.Create);
        descriptor.AddFieldHandler(QueryableEnumInHandler.Create);
        descriptor.AddFieldHandler(QueryableEnumNotInHandler.Create);

        descriptor.AddFieldHandler(QueryableListAnyOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableListAllOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableListNoneOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableListSomeOperationHandler.Create);

        descriptor.AddFieldHandler(QueryableDataOperationHandler.Create);
        descriptor.AddFieldHandler(QueryableDefaultFieldHandler.Create);

        return descriptor;
    }
}
