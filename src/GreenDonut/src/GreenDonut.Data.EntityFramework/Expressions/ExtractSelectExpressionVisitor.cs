using System.Linq.Expressions;

namespace GreenDonut.Data.Expressions;

internal sealed class ExtractSelectExpressionVisitor : ExpressionVisitor
{
    private const string SelectMethod = "Select";

    public LambdaExpression? Selector { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == SelectMethod && node.Arguments.Count == 2)
        {
            var lambda = ConvertToLambda(node.Arguments[1]);
            if (lambda.Type.IsGenericType
                && lambda.Type.GetGenericTypeDefinition() == typeof(Func<,>))
            {
                // we make sure that the selector is of type Expression<Func<T, T>>
                // otherwise we are not interested in it.
                var genericArgs = lambda.Type.GetGenericArguments();
                if (genericArgs[0] == genericArgs[1])
                {
                    Selector = lambda;
                }
            }
        }

        return base.VisitMethodCall(node);
    }

    private static LambdaExpression ConvertToLambda(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }

        if (e.NodeType != ExpressionType.MemberAccess)
        {
            return (LambdaExpression)e;
        }

        // Convert the property expression into a lambda expression
        var typeArguments = e.Type.GetGenericArguments()[0].GetGenericArguments();
        return Expression.Lambda(e, Expression.Parameter(typeArguments[0]));
    }
}
