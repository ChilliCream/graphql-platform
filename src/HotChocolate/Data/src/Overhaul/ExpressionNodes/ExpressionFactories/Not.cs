using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

[NoStructuralDependencies]
public sealed class Not : IExpressionFactory
{
    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var children = context.Expressions.Children;
        var expression = children[0];
        return Expression.Not(expression);
    }
}
