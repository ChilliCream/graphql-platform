using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

[NoStructuralDependencies]
public sealed class Select : IExpressionFactory
{
    public static readonly Select Instance = new();

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        // I don't like the fact that here we work with assumptions rather than concrete types.
        // Can anything be done here?
        var children = context.Expressions.Children;
        var memberAccessLike = children[0];
        var projection = children[1];

        var methodCache = EnumerableMethodCache.Select2;
        // member.Select(projection)
        var select = Expression.Call(
            // TODO: won't work, because memberAccessLike.Type is IEnumerable of the type we want.
            methodCache.GetMethod(memberAccessLike.Type, projection.Type),
            memberAccessLike,
            projection);
        return select;
    }
}
