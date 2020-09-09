using HotChocolate.Data.Sorting.Expressions;

namespace HotChocolate.Data.Sorting
{
    public static class SortConventionDescriptorQueryableExtensions
    {
        public static ISortConventionDescriptor UseQueryableProvider(
            this ISortConventionDescriptor descriptor) =>
            descriptor.Provider(new QueryableSortProvider(x => x.AddDefaultFieldHandlers()));

        public static ISortProviderDescriptor<QueryableSortContext> AddDefaultFieldHandlers(
            this ISortProviderDescriptor<QueryableSortContext> descriptor)
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
