using System.Linq.Expressions;

namespace GreenDonut.Data.Expressions;

internal sealed class OrderByRemovalRewriter : ExpressionVisitor
{
    private readonly List<LambdaExpression> _orderExpressions = [];
    private readonly List<string> _orderMethods = [];

    public (Expression, List<LambdaExpression>, List<string>) Rewrite(Expression expression)
    {
        var result = Visit(expression);

        _orderExpressions.Reverse();
        _orderMethods.Reverse();

        return (result, _orderExpressions, _orderMethods);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Queryable)
            && (node.Method.Name == nameof(Queryable.OrderBy)
                || node.Method.Name == nameof(Queryable.OrderByDescending)
                || node.Method.Name == nameof(Queryable.ThenBy)
                || node.Method.Name == nameof(Queryable.ThenByDescending)))
        {
            var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
            _orderExpressions.Add(lambda);
            _orderMethods.Add(node.Method.Name);
            return Visit(node.Arguments[0]);
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitLambda<T>(Expression<T> node) => node;

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }

        return e;
    }
}
