#if NET8_0_OR_GREATER
using System.Linq.Expressions;

namespace GreenDonut.Projections;

internal static class ExpressionHelpers
{
    public static Expression<Func<T, T>> Combine<T>(
        Expression<Func<T, T>> first,
        Expression<Func<T, T>> second)
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        var firstBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var secondBody = ReplaceParameter(second.Body, second.Parameters[0], firstBody);
        return Expression.Lambda<Func<T, T>>(secondBody, parameter);
    }

    private static Expression ReplaceParameter(
        Expression body,
        ParameterExpression toReplace,
        Expression replacement)
        => new ParameterReplacer(toReplace, replacement).Visit(body);

    private class ParameterReplacer(
        ParameterExpression toReplace,
        Expression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == toReplace ? replacement : base.VisitParameter(node);
    }
}
#endif
