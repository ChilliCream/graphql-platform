using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters.Expressions
{
    public static class QueryableFilterExtensions
    {
        public static IEnumerable<T> Filter<T>(
            this IEnumerable<T> enumerable,
            IResolverContext context) =>
            ExecuteFilter(enumerable, context, typeof(IEnumerable<T>));

        public static QueryableExecutable<T> Filter<T>(
            this QueryableExecutable<T> enumerable,
            IResolverContext context) =>
            ExecuteFilter(enumerable, context, typeof(QueryableExecutable<T>));

        private static T ExecuteFilter<T>(
            this T input,
            IResolverContext context,
            Type expectedType)
        {
            if (context.LocalContextData.TryGetValue(
                    QueryableFilterProvider.ContextApplyFilteringKey,
                    out object? applicatorObj) &&
                applicatorObj is ApplyFiltering applicator)
            {
                var resultObj = applicator(context, input);
                if (resultObj is T result)
                {
                    return result;
                }

                throw ThrowHelper.Filtering_TypeMissmatch(
                    context,
                    expectedType,
                    resultObj!.GetType());
            }

            throw ThrowHelper.Filtering_FilteringWasNotFound(context);
        }
    }
}
