using System.Linq.Expressions;
using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Filters
{
    public static class FilterConventionDescriptorQueryableExtensions
    {
        public static IFilterConventionDescriptor UseQueryableProvider(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.Provider(new QueryableFilterProvider(x => x.UseDefaults()));

        public static IFilterProviderDescriptor<Expression, QueryableFilterContext> UseDefaults(
            this IFilterProviderDescriptor<Expression, QueryableFilterContext> descriptor)
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

            descriptor.AddFieldHandler<QueryableListAnyOperationHandler>();
            descriptor.AddFieldHandler<QueryableListAllOperationHandler>();
            descriptor.AddFieldHandler<QueryableListNoneOperationHandler>();
            descriptor.AddFieldHandler<QueryableListSomeOperationHandler>();

            descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
            descriptor.Visitor<FilterVisitor<Expression, QueryableFilterContext>>();
            descriptor.Combinator<QueryableCombinator>();
            return descriptor;
        }
    }
}