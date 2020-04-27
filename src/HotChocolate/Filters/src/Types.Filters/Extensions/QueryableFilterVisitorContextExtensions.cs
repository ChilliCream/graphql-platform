using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters
{
    public static class QueryableFilterVisitorContextExtensions
    {
        public static void ReportError(
            this IQueryableFilterVisitorContext context,
            IError error) =>
                context.Errors.Add(error);

        public static QueryableClosure AddClosure(
            this IQueryableFilterVisitorContext context,
            Type type) =>
                context.AddClosure(type, "_s" + context.Closures.Count, context.InMemory);

        public static QueryableClosure AddIsNullClosure(
            this IQueryableFilterVisitorContext context,
            Type type)
        {
            QueryableClosure closure =
                context.AddClosure(type, "_s" + context.Closures.Count, false);
            context.GetLevel().Enqueue(FilterExpressionBuilder.Equals(closure.Parameter, null));
            return closure;
        }

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
            this IQueryableFilterVisitorContext context) =>
                context.Closures.Peek();

        public static Expression GetInstance(
            this IQueryableFilterVisitorContext context) =>
                context.Closures.Peek().Instance.Peek();

        public static Queue<Expression> GetLevel(
            this IQueryableFilterVisitorContext context) =>
                context.Closures.Peek().Level.Peek();

        public static void PushInstance(
            this IQueryableFilterVisitorContext context, Expression nextExpression) =>
                context.Closures.Peek().Instance.Push(nextExpression);

        public static void PushLevel(
            this IQueryableFilterVisitorContext context, Queue<Expression> nextLevel) =>
                context.Closures.Peek().Level.Push(nextLevel);

        public static Expression PopInstance(
            this IQueryableFilterVisitorContext context) =>
                context.Closures.Peek().Instance.Pop();

        public static Queue<Expression> PopLevel(
            this IQueryableFilterVisitorContext context) =>
                context.Closures.Peek().Level.Pop();

        public static QueryableClosure PopClosure(
            this IQueryableFilterVisitorContext context) =>
                context.Closures.Pop();

        public static bool TryCreateLambda<TSource>(
            this IQueryableFilterVisitorContext context,
            [NotNullWhen(true)]out Expression<Func<TSource, bool>>? expression) =>
                context.GetClosure().TryCreateLambda(out expression);

        public static bool TryCreateLambda(
            this IQueryableFilterVisitorContext context,
            [NotNullWhen(true)]out LambdaExpression? expression) =>
                context.GetClosure().TryCreateLambda(out expression);
    }
}
