using System.Linq.Expressions;

namespace GreenDonut.Data.Expressions;

internal sealed class ExtractOrderPropertiesVisitor : ExpressionVisitor
{
    private const string OrderByMethod = "OrderBy";
    private const string ThenByMethod = "ThenBy";
    private const string OrderByDescendingMethod = "OrderByDescending";
    private const string ThenByDescendingMethod = "ThenByDescending";
    public List<MemberExpression> OrderProperties { get; } = [];

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == OrderByMethod
            || node.Method.Name == ThenByMethod
            || node.Method.Name == OrderByDescendingMethod
            || node.Method.Name == ThenByDescendingMethod)
        {
            var lambda = StripQuotes(node.Arguments[1]);
            new OrderPropertyVisitor(lambda.Parameters[0], OrderProperties).Visit(lambda.Body);
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitLambda<T>(Expression<T> node) => node;

    private sealed class OrderPropertyVisitor(
        ParameterExpression orderKeyParameter,
        List<MemberExpression> orderProperties)
        : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression == orderKeyParameter)
            {
                orderProperties.Add(node);
            }

            return base.VisitMember(node);
        }
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
