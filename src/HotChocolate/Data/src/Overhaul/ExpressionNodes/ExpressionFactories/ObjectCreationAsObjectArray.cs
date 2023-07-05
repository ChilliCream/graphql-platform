using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

[NoStructuralDependencies]
public sealed class ObjectCreationAsObjectArray : IExpressionFactory
{
    public static readonly ObjectCreationAsObjectArray Instance = new();
    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var children = context.Expressions.Children;
        // If the casts are not required, this is all that's needed.
        var array = Expression.NewArrayInit(typeof(object), children);
        return array;
    }
}
