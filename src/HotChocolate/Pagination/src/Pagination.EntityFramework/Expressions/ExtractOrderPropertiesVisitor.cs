using System.Linq.Expressions;

namespace HotChocolate.Pagination.Expressions;

internal sealed class ExtractOrderPropertiesVisitor : ExpressionVisitor
{
    private const string _orderByMethod = "OrderBy";
    private const string _thenByMethod = "ThenBy";
    private const string _orderByDescendingMethod = "OrderByDescending";
    private const string _thenByDescendingMethod = "ThenByDescending";
    private bool _isOrderScope;

    public List<MemberExpression> OrderProperties { get; } = [];

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == _orderByMethod ||
            node.Method.Name == _thenByMethod ||
            node.Method.Name == _orderByDescendingMethod ||
            node.Method.Name == _thenByDescendingMethod)
        {
            _isOrderScope = true;

            var lambda = StripQuotes(node.Arguments[1]);
            Visit(lambda.Body);

            _isOrderScope = false;
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (_isOrderScope)
        {
            // we only collect members that are within an order method.
            OrderProperties.Add(node);
        }

        return base.VisitMember(node);
    }

    private static LambdaExpression StripQuotes(Expression expression)
    {
        while (expression.NodeType == ExpressionType.Quote)
        {
            expression = ((UnaryExpression)expression).Operand;
        }

        return (LambdaExpression)expression;
    }
}
