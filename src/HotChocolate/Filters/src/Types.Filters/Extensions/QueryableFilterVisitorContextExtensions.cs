using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters
{
    public static class QueryableFilterVisitorContextExtensions
    {
        public static QueryableClosure AddClosure(
            this IQueryableFilterVisitorContext context,
            Type type)
                => context.AddClosure(type, "_s" + context.Closures.Count, context.InMemory);

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

        public static QueryableClosure GetClosure(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Peek();

        public static Expression GetInstance(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Peek().Instance.Peek();

        public static Queue<Expression> GetLevel(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Peek().Level.Peek();

        public static void PushInstance(
            this IQueryableFilterVisitorContext context, Expression nextExpression)
                => context.Closures.Peek().Instance.Push(nextExpression);

        public static void PushLevel(
            this IQueryableFilterVisitorContext context, Queue<Expression> nextLevel)
                => context.Closures.Peek().Level.Push(nextLevel);

        public static Expression PopInstance(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Peek().Instance.Pop();

        public static Queue<Expression> PopLevel(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Peek().Level.Pop();

        public static QueryableClosure PopClosure(
            this IQueryableFilterVisitorContext context)
                => context.Closures.Pop();

        public static Expression<Func<TSource, bool>> CreateFilter<TSource>(
            this IQueryableFilterVisitorContext context)
                => context.GetClosure().CreateLambda<Func<TSource, bool>>();

        public static Expression CreateFilter(
            this IQueryableFilterVisitorContext context)
                => context.GetClosure().CreateLambda();
    }
}
