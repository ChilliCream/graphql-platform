using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters
{
    public static class FilterVisitorContextExtensions
    {
        public static void ReportError<T>(this IFilterVisitorContext<T> context, IError error) =>
                context.Errors.Add(error);

        public static FilterScope<T> AddScope<T>(
            this IFilterVisitorContext<T> context)
        {
            FilterScope<T>? closure = context.CreateScope();
            context.Scopes.Push(closure);
            return closure;
        }

        public static FilterScope<T> GetScope<T>(
            this IFilterVisitorContext<T> context) =>
            context.Scopes.Peek();

        public static T GetInstance<T>(
            this IFilterVisitorContext<T> context) =>
            context.Scopes.Peek().Instance.Peek();

        public static Queue<T> GetLevel<T>(
            this IFilterVisitorContext<T> context) =>
            context.Scopes.Peek().Level.Peek();

        public static void PushInstance<T>(
            this IFilterVisitorContext<T> context, T nextExpression) =>
            context.Scopes.Peek().Instance.Push(nextExpression);

        public static void PushLevel<T>(
            this IFilterVisitorContext<T> context, Queue<T> nextLevel) =>
            context.Scopes.Peek().Level.Push(nextLevel);

        public static T PopInstance<T>(
            this IFilterVisitorContext<T> context) =>
            context.Scopes.Peek().Instance.Pop();

        public static Queue<T> PopLevel<T>(
            this IFilterVisitorContext<T> context) =>
            context.Scopes.Peek().Level.Pop();

        public static FilterScope<T> PopScope<T>(
            this IFilterVisitorContext<T> context) =>
            context.Scopes.Pop();
    }

    public static class QueryableFilterVisitorContextExtensions
    {
        public static FilterScope<Expression> AddIsNullClosure(
            this QueryableFilterVisitorContext context)
        {
            var closure = new QueryableScope(
                context.ClrTypes.Peek(), "_s" + context.Scopes.Count, false);

            context.Scopes.Push(closure);

            context.GetLevel().Enqueue(
                FilterExpressionBuilder.Equals(context.GetClosure().Parameter, null));

            return closure;
        }

        public static QueryableScope GetClosure(
                this QueryableFilterVisitorContext context) =>
                    (QueryableScope)context.GetScope();

        public static bool TryCreateLambda<TSource>(
           this QueryableFilterVisitorContext context,
           [NotNullWhen(true)] out Expression<Func<TSource, bool>>? expression) =>
                context.GetClosure().TryCreateLambda(out expression);

        public static bool TryCreateLambda(
            this QueryableFilterVisitorContext context,
            [NotNullWhen(true)] out LambdaExpression? expression) =>
                context.GetClosure().TryCreateLambda(out expression);
    }
}
