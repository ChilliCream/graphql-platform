using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace HotChocolate.Data.Projections.Expressions
{
    public static class QueryableProjectionContextExtensions
    {
        public static QueryableProjectionScope AddScope(
            this QueryableProjectionContext context,
            Type runtimeType)
        {
            var parameterName = "p" + context.Scopes.Count;
            var closure =
                new QueryableProjectionScope(runtimeType, parameterName);
            context.Scopes.Push(closure);
            return closure;
        }

        public static bool TryGetQueryableScope(
            this QueryableProjectionContext ctx,
            [NotNullWhen(true)] out QueryableProjectionScope? scope)
        {
            if (ctx.Scopes.Count > 0 &&
                ctx.Scopes.Peek() is QueryableProjectionScope queryableScope)
            {
                scope = queryableScope;
                return true;
            }

            scope = null;
            return false;
        }

        public static Expression<Func<T, T>> Project<T>(this QueryableProjectionContext context)
        {
            if (context.TryGetQueryableScope(out QueryableProjectionScope? scope))
            {
                return scope.Project<T>();
            }

            // TODO
            throw new InvalidOperationException();
        }
    }
}
