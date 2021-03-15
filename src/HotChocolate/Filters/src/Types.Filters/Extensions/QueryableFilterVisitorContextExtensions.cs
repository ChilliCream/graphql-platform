using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public static class QueryableFilterVisitorContextExtensions
    {
        [Obsolete("Use HotChocolate.Data.")]
        public static QueryableClosure AddClosure(
            this IQueryableFilterVisitorContext context,
            Type type)
                => context.AddClosure(type, "_s" + context.Closures.Count, context.InMemory);

        [Obsolete("Use HotChocolate.Data.")]
        public static QueryableClosure AddClosure(
            this IQueryableFilterVisitorContext context,
            Type type,
            string parameterName,
            bool inMemory)
        {
            var closure = new QueryableClosure(type, parameterName, inMemory);
            context.Closures.Push(closure);
            return closure;
        }

        [Obsolete("Use HotChocolate.Data.")]
        public static QueryableClosure GetClosure(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Peek();

        [Obsolete("Use HotChocolate.Data.")]
        public static Expression GetInstance(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Peek().Instance.Peek();

        [Obsolete("Use HotChocolate.Data.")]
        public static Queue<Expression> GetLevel(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Peek().Level.Peek();

        [Obsolete("Use HotChocolate.Data.")]
        public static void PushInstance(
            this IQueryableFilterVisitorContext context, Expression nextExpression)
                => context.Closures.Peek().Instance.Push(nextExpression);

        [Obsolete("Use HotChocolate.Data.")]
        public static void PushLevel(
            this IQueryableFilterVisitorContext context, Queue<Expression> nextLevel)
                => context.Closures.Peek().Level.Push(nextLevel);

        [Obsolete("Use HotChocolate.Data.")]
        public static Expression PopInstance(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Peek().Instance.Pop();

        [Obsolete("Use HotChocolate.Data.")]
        public static Queue<Expression> PopLevel(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Peek().Level.Pop();

        [Obsolete("Use HotChocolate.Data.")]
        public static QueryableClosure PopClosure(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Pop();

        [Obsolete("Use HotChocolate.Data.")]
        public static Expression<Func<TSource, bool>> CreateFilter<TSource>(
            this IQueryableFilterVisitorContext context)
                => context.GetClosure().CreateLambda<Func<TSource, bool>>();

        [Obsolete("Use HotChocolate.Data.")]
        public static Expression CreateFilter(
            this IQueryableFilterVisitorContext context)
                => context.GetClosure().CreateLambda();
    }
}
