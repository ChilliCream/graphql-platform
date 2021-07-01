using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Expressions
{
    /// <summary>
    /// Extensions for projection for <see cref="IEnumerable{T}"/>
    /// </summary>
    public static class QueryableProjectExtensions
    {
        /// <summary>
        /// Projects the selection set of the request onto the enumerable.
        /// </summary>
        /// <param name="enumerable">The enumerable</param>
        /// <param name="context">
        /// The resolver context of the resolver that is annotated with UseProjection
        /// </param>
        /// <returns>The projected enumerable</returns>
        public static IEnumerable<T> Project<T>(
            this IEnumerable<T> enumerable,
            IResolverContext context) =>
            ExecuteProject(enumerable, context, typeof(IEnumerable<T>));

        /// <summary>
        /// Projects the selection set of the request onto the enumerable.
        /// </summary>
        /// <param name="enumerable">The enumerable</param>
        /// <param name="context">
        /// The resolver context of the resolver that is annotated with UseProjection
        /// </param>
        /// <returns>The projected enumerable</returns>
        public static QueryableExecutable<T> Project<T>(
            this QueryableExecutable<T> enumerable,
            IResolverContext context) =>
            ExecuteProject(enumerable, context, typeof(QueryableExecutable<T>));

        private static T ExecuteProject<T>(
            this T input,
            IResolverContext context,
            Type expectedType)
        {
            if (context.LocalContextData.TryGetValue(
                    QueryableProjectionProvider.ContextApplyProjectionKey,
                    out object? applicatorObj) &&
                applicatorObj is ApplyProjection applicator)
            {
                var resultObj = applicator(context, input);
                if (resultObj is T result)
                {
                    return result;
                }

                throw ThrowHelper.Projection_TypeMissmatch(
                    context,
                    expectedType,
                    resultObj!.GetType());
            }

            throw ThrowHelper.Projection_ProjectionWasNotFound(context);
        }
    }
}
