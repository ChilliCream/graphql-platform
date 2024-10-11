using System.Linq.Expressions;

namespace HotChocolate.Pagination.Expressions;

internal sealed class ReplaceSelectorVisitor<T>(
    Expression<Func<T, T>> newSelector)
    : ExpressionVisitor
{
    private const string _selectMethod = "Select";

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == _selectMethod && node.Arguments.Count == 2)
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
