using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters
{
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
