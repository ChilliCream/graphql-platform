using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Utilities;
using System.Linq.Expressions;

namespace HotChocolate.Data.Filters.Expressions
{
    public static class QueryableFilterVisitorContextExtensions
    {
        public static FilterScope<Expression> AddIsNullClosure(
            this QueryableFilterContext context)
        {
            var closure = new QueryableScope(
                context.RuntimeTypes.Peek().Source,
                "_s" + context.Scopes.Count,
                false);

            context.Scopes.Push(closure);

            context.GetLevel()
                .Enqueue(
                    FilterExpressionBuilder.Equals(context.GetClosure().Parameter, null));

            return closure;
        }

        public static QueryableScope GetClosure(
            this QueryableFilterContext context) =>
            (QueryableScope)context.GetScope();

        public static bool TryCreateLambda(
            this QueryableFilterContext context,
            [NotNullWhen(true)] out LambdaExpression? expression)
        {
            if (context.Scopes.TryPeekElement(out FilterScope<Expression>? scope) &&
                scope is QueryableScope closure &&
                closure.Level.TryPeekElement(out Queue<Expression>? levels) &&
                levels.TryPeekElement(out Expression? level))
            {
                expression = Expression.Lambda(level, closure.Parameter);
                return true;
            }

            expression = null;
            return false;
        }

        public static bool TryCreateLambda<T>(
            this QueryableFilterContext context,
            [NotNullWhen(true)] out Expression<T>? expression)
        {
            if (context.Scopes.TryPeekElement(out FilterScope<Expression>? scope) &&
                scope is QueryableScope closure &&
                closure.Level.TryPeekElement(out Queue<Expression>? levels) &&
                levels.TryPeekElement(out Expression? level))
            {
                expression = Expression.Lambda<T>(level, closure.Parameter);
                return true;
            }

            expression = null;
            return false;
        }
    }
}
