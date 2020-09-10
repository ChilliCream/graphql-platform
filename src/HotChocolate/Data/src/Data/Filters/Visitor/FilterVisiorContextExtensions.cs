using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public static class FilterVisitorContextExtensions
    {
        public static void ReportError<T>
            (this IFilterVisitorContext<T> context,
            IError error) =>
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

        public static Queue<T> GetLevel<T>(this IFilterVisitorContext<T> context) =>
            context.Scopes.Peek().Level.Peek();

        public static void PushInstance<T>(
            this IFilterVisitorContext<T> context, T nextExpression) =>
            context.Scopes.Peek().Instance.Push(nextExpression);

        public static void PushLevel<T>(
            this IFilterVisitorContext<T> context, Queue<T> nextLevel) =>
            context.Scopes.Peek().Level.Push(nextLevel);

        public static T PopInstance<T>(this IFilterVisitorContext<T> context) =>
            context.Scopes.Peek().Instance.Pop();

        public static Queue<T> PopLevel<T>(this IFilterVisitorContext<T> context) =>
            context.Scopes.Peek().Level.Pop();

        public static FilterScope<T> PopScope<T>(
            this IFilterVisitorContext<T> context) =>
            context.Scopes.Pop();
    }
}
