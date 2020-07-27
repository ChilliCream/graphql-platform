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
            descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
            descriptor.Visitor<FilterVisitor<Expression, QueryableFilterContext>>();
            descriptor.Combinator<QueryableCombinator>();
            return descriptor;
        }
    }
}