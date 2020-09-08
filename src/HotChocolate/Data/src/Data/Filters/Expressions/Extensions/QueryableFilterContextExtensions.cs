using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace HotChocolate.Data.Filters.Expressions
{
    public static class QueryableFilterVisitorContextExtensions
    {
        public static FilterScope<Expression> AddIsNullClosure(
            this QueryableFilterContext context)
        {
            var closure = new QueryableScope(
                context.RuntimeTypes.Peek(), "_s" + context.Scopes.Count, false);

            context.Scopes.Push(closure);

            context.GetLevel().Enqueue(
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
            if (context.Scopes.Count > 0 &&
                context.Scopes.Peek() is QueryableScope closure)
            {
                expression = null;

                if (closure.Level.Peek().Count == 0)
                {
                    return false;
                }

                expression = Expression.Lambda(closure.Level.Peek().Peek(), closure.Parameter);
                return true;
            }

            expression = null;
            return false;
        }

        public static bool TryCreateLambda<T>(
            this QueryableFilterContext context,
            [NotNullWhen(true)] out Expression<T>? expression)
        {
            if (context.Scopes.Count > 0 &&
                context.Scopes.Peek() is QueryableScope closure)
            {
                expression = null;

                if (closure.Level.Peek().Count == 0)
                {
                    return false;
                }

                expression = Expression.Lambda<T>(closure.Level.Peek().Peek(), closure.Parameter);

                return true;
            }

            expression = null;
            return false;
        }
    }
}
