using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters.Expressions
{
    /// <summary>
    /// Extensions for filtering for <see cref="IEnumerable{T}"/>
    /// </summary>
    public static class QueryableFilterExtensions
    {
        /// <summary>
        /// Filters the selection set of the request onto the enumerable.
        /// </summary>
        /// <param name="enumerable">The enumerable</param>
        /// <param name="context">
        /// The resolver context of the resolver that is annotated with UseFiltering
        /// </param>
        /// <returns>The filtered enumerable</returns>
        public static IEnumerable<T> Filter<T>(
            this IEnumerable<T> enumerable,
            IResolverContext context) =>
            ExecuteFilter(enumerable, context, typeof(IEnumerable<T>));

        /// <summary>
        /// Filters the selection set of the request onto the enumerable.
        /// </summary>
        /// <param name="enumerable">The enumerable</param>
        /// <param name="context">
        /// The resolver context of the resolver that is annotated with UseFiltering
        /// </param>
        /// <returns>The filtered enumerable</returns>
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
