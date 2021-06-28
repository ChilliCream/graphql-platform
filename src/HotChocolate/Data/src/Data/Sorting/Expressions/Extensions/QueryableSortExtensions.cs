using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Sorting.Expressions
{
    public static class QueryableSortExtensions
    {
        public static IEnumerable<T> Sort<T>(
            this IEnumerable<T> enumerable,
            IResolverContext context) =>
            ExecuteSort(enumerable, context, typeof(IEnumerable<T>));

        public static QueryableExecutable<T> Sort<T>(
            this QueryableExecutable<T> enumerable,
            IResolverContext context) =>
            ExecuteSort(enumerable, context, typeof(QueryableExecutable<T>));

        private static T ExecuteSort<T>(
            this T input,
            IResolverContext context,
            Type expectedType)
        {
            if (context.LocalContextData.TryGetValue(
                    QueryableSortProvider.ContextApplySortingKey,
                    out object? applicatorObj) &&
                applicatorObj is ApplySorting applicator)
            {
                var resultObj = applicator(context, input);
                if (resultObj is T result)
                {
                    return result;
                }

                throw ThrowHelper.Sorting_TypeMissmatch(
                    context,
                    expectedType,
                    resultObj!.GetType());
            }

            throw ThrowHelper.Sorting_SortingWasNotFound(context);
        }
    }
}
