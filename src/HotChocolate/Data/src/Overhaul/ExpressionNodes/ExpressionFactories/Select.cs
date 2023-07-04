using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class Select : IExpressionFactory
{
    private static readonly MethodInfo SelectMethod =
        typeof(Enumerable).GetMethods()
            .Single(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2);

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        // I don't like the fact that here we work with assumptions rather than concrete types.
        // Can anything be done here?
        var children = context.Expressions.Children;
        var memberAccessLike = children[0];
        var projection = children[1];

        // member.Select(projection)
        var select = Expression.Call(
            SelectMethod.MakeGenericMethod(memberAccessLike.Type, projection.Type),
            memberAccessLike,
            projection);
        return select;
    }
}
