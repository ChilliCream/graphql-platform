using System.Linq.Expressions;

namespace GreenDonut.Data.Expressions;

internal sealed class ExtractOrderPropertiesVisitor : QueryChainVisitor
{
    private const string OrderByMethod = "OrderBy";
    private const string ThenByMethod = "ThenBy";
    private const string OrderByDescendingMethod = "OrderByDescending";
    private const string ThenByDescendingMethod = "ThenByDescending";
    private ParameterExpression? _orderKeyParameter;

    public List<MemberExpression> OrderProperties { get; } = [];

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (_orderKeyParameter is null
            && (node.Method.Name == OrderByMethod
                || node.Method.Name == ThenByMethod
                || node.Method.Name == OrderByDescendingMethod
                || node.Method.Name == ThenByDescendingMethod))
        {
            var lambda = StripQuotes(node.Arguments[1]);

            _orderKeyParameter = lambda.Parameters[0];
            Visit(lambda.Body);
            _orderKeyParameter = null;
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // an order key is a lambda. we only collect members rooted at that lambda's own
        // parameter; members rooted on nested lambdas or intermediate results cannot be
        // hoisted into the selector.
        if (_orderKeyParameter is not null && node.Expression == _orderKeyParameter)
        {
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
