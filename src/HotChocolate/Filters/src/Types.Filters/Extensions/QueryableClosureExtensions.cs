using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters
{
    public static class QueryableClosureExtensions
    {
        public static bool TryCreateLambda(
            this QueryableClosure closure,
            [NotNullWhen(true)]out LambdaExpression? expression)
        {
            expression = null;
            if (closure.Level.Peek().Count == 0)
            {
                return false;
            }

            expression = closure.InMemory ?
                Expression.Lambda(closure.GetExpressionBodyWithNullCheck(), closure.Parameter) :
                Expression.Lambda(closure.Level.Peek().Peek(), closure.Parameter);

            return true;
        }

        public static bool TryCreateLambda<T>(
            this QueryableClosure closure,
            [NotNullWhen(true)]out Expression<T>? expression)
        {
            expression = null;
            if (closure.Level.Peek().Count == 0)
            {
                return false;
            }

            expression = closure.InMemory ?
                Expression.Lambda<T>(closure.GetExpressionBodyWithNullCheck(), closure.Parameter) :
                Expression.Lambda<T>(closure.Level.Peek().Peek(), closure.Parameter);

            return true;
        }

        private static Expression GetExpressionBodyWithNullCheck(this QueryableClosure closure)
            => FilterExpressionBuilder.NotNullAndAlso(
                closure.Parameter, closure.Level.Peek().Peek());
    }


}
