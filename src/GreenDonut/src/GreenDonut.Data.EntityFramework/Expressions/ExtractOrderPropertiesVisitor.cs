using System.Linq.Expressions;

namespace GreenDonut.Data.Expressions;

internal sealed class ExtractOrderPropertiesVisitor : ExpressionVisitor
{
    private const string OrderByMethod = "OrderBy";
    private const string ThenByMethod = "ThenBy";
    private const string OrderByDescendingMethod = "OrderByDescending";
    private const string ThenByDescendingMethod = "ThenByDescending";
    private bool _isOrderScope;

    public List<MemberExpression> OrderProperties { get; } = [];

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == OrderByMethod
            || node.Method.Name == ThenByMethod
            || node.Method.Name == OrderByDescendingMethod
            || node.Method.Name == ThenByDescendingMethod)
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
