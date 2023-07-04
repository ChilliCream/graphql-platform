using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

// TODO:
// This needs special treatment, because it can cut off a branch from rendering.
// I guess things like this will happen quite often when structural dependencies are involved.
// It needs some sort of "not needed when" expression to be able to cache it properly.
// Let's leave that for later, since it's not essential (structural dependencies as a whole is a rarity).
public sealed class HideChildOptionally : IExpressionFactory
{
    private readonly Box<bool> _hideVariable;

    public HideChildOptionally(Box<bool> hideVariable)
    {
        _hideVariable = hideVariable;
    }

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        bool hide = _hideVariable.Value;
        if (hide)
            return Expression.Default(context.ExpectedExpressionType);
        return context.Expressions.Children[0];
    }
}
