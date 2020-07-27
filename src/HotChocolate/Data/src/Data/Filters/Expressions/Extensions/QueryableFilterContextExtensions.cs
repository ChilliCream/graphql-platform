using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public static class QueryableFilterVisitorContextExtensions
    {
        public static FilterScope<Expression> AddIsNullClosure(
            this QueryableFilterContext context)
        {
            var closure = new QueryableScope(
                context.ClrTypes.Peek(), "_s" + context.Scopes.Count, false);

            context.Scopes.Push(closure);

            context.GetLevel().Enqueue(
                FilterExpressionBuilder.Equals(context.GetClosure().Parameter, null));

            return closure;
        }

        public static QueryableScope GetClosure(
                this QueryableFilterContext context) =>
                    (QueryableScope)context.GetScope();

        public static bool TryCreateLambda<TSource>(
           this QueryableFilterContext context,
           [NotNullWhen(true)] out Expression<Func<TSource, bool>>? expression) =>
                context.GetClosure().TryCreateLambda(out expression);

        public static bool TryCreateLambda(
            this QueryableFilterContext context,
            [NotNullWhen(true)] out LambdaExpression? expression) =>
                context.GetClosure().TryCreateLambda(out expression);

        public static bool TryGetParentField(
           this QueryableFilterContext context,
           [NotNullWhen(true)] out IFilterField? field)
        {
            if (context.Operations.TryPeekAt(1, out IInputField? parentField) &&
                parentField is IFilterField parentFilterField)
            {
                field = parentFilterField;
                return true;
            }
            field = default;
            return false;
        }
    }
}