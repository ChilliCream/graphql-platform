using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class BoxValueType : IExpressionFactory
{
    public static readonly BoxValueType Instance = new();

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var child = context.Expressions.Children[0];
        return Expression.Convert(child, typeof(object));
    }
}
