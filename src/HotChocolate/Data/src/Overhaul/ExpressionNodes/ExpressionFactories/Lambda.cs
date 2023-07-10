using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

// This is needed in order to rewrap the instance.
// It doesn't make sense for the instance to suddenly point at the inner argument in Select.
// rootInstance => child[0](instance)
// Can also be used in method where you pass in a delegate (e.g. Where).
[NoStructuralDependencies]
public sealed class Lambda : IExpressionFactory
{
    public static readonly Lambda Instance = new();
    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var instance = context.Expressions.InnerInstance!;
        var projection = context.Expressions.Children[0];
        var lambda = Expression.Lambda(projection, instance);
        return lambda;
    }
}
