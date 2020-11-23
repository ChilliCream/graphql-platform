using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data
{
    public static class FilterConventionDescriptorQueryableExtensions
    {
        public static IFilterConventionDescriptor UseQueryableProvider(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.Provider(new QueryableFilterProvider(x => x.AddDefaultFieldHandlers()));

        public static IFilterProviderDescriptor<QueryableFilterContext> AddDefaultFieldHandlers(
            this IFilterProviderDescriptor<QueryableFilterContext> descriptor)
        {
            descriptor.AddFieldHandler<QueryableBooleanEqualsHandler>();
            descriptor.AddFieldHandler<QueryableBooleanNotEqualsHandler>();

            descriptor.AddFieldHandler<QueryableComparableEqualsHandler>();
            descriptor.AddFieldHandler<QueryableComparableNotEqualsHandler>();
            descriptor.AddFieldHandler<QueryableComparableInHandler>();
            descriptor.AddFieldHandler<QueryableComparableNotInHandler>();
            descriptor.AddFieldHandler<QueryableComparableGreaterThanHandler>();
            descriptor.AddFieldHandler<QueryableComparableNotGreaterThanHandler>();
            descriptor.AddFieldHandler<QueryableComparableGreaterThanOrEqualsHandler>();
            descriptor.AddFieldHandler<QueryableComparableNotGreaterThanOrEqualsHandler>();
            descriptor.AddFieldHandler<QueryableComparableLowerThanHandler>();
            descriptor.AddFieldHandler<QueryableComparableNotLowerThanHandler>();
            descriptor.AddFieldHandler<QueryableComparableLowerThanOrEqualsHandler>();
            descriptor.AddFieldHandler<QueryableComparableNotLowerThanOrEqualsHandler>();

            descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
            descriptor.AddFieldHandler<QueryableStringNotEqualsHandler>();
            descriptor.AddFieldHandler<QueryableStringStartsWithHandler>();
            descriptor.AddFieldHandler<QueryableStringNotStartsWithHandler>();
            descriptor.AddFieldHandler<QueryableStringEndsWithHandler>();
            descriptor.AddFieldHandler<QueryableStringNotEndsWithHandler>();
            descriptor.AddFieldHandler<QueryableStringInHandler>();
            descriptor.AddFieldHandler<QueryableStringNotInHandler>();
            descriptor.AddFieldHandler<QueryableStringContainsHandler>();
            descriptor.AddFieldHandler<QueryableStringNotContainsHandler>();

            descriptor.AddFieldHandler<QueryableEnumEqualsHandler>();
            descriptor.AddFieldHandler<QueryableEnumNotEqualsHandler>();
            descriptor.AddFieldHandler<QueryableEnumInHandler>();
            descriptor.AddFieldHandler<QueryableEnumNotInHandler>();

            descriptor.AddFieldHandler<QueryableListAnyOperationHandler>();
            descriptor.AddFieldHandler<QueryableListAllOperationHandler>();
            descriptor.AddFieldHandler<QueryableListNoneOperationHandler>();
            descriptor.AddFieldHandler<QueryableListSomeOperationHandler>();

            descriptor.AddFieldHandler<QueryableDataOperationHandler>();
            descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();

            return descriptor;
        }
    }
}
