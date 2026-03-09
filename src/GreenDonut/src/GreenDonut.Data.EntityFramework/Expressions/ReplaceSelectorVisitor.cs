using System.Linq.Expressions;

namespace GreenDonut.Data.Expressions;

internal sealed class ReplaceSelectorVisitor<T>(
    Expression<Func<T, T>> newSelector)
    : ExpressionVisitor
{
    private const string SelectMethod = "Select";

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == SelectMethod && node.Arguments.Count == 2)
        {
            return Expression.Call(
                node.Method.DeclaringType!,
                node.Method.Name,
                [typeof(T), typeof(T)],
                node.Arguments[0],
                newSelector);
        }

        return base.VisitMethodCall(node);
    }
}
