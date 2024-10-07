using System.Linq.Expressions;

namespace HotChocolate.Pagination.Expressions;

internal sealed class ExtractSelectExpressionVisitor : ExpressionVisitor
{
    private const string _selectMethod = "Select";

    public LambdaExpression? Selector { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == _selectMethod && node.Arguments.Count == 2)
        {
            var lambda = StripQuotes(node.Arguments[1]);
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

    private static LambdaExpression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }

        return (LambdaExpression)e;
    }
}
