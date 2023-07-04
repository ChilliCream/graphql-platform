using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

// TODO:
// This needs special treatment, because it can cut off a branch from rendering.
// I guess things like this will happen quite often when structural dependencies are involved.
// It needs some sort of "not needed when" expression to be able to cache it properly.
// Let's leave that for later, since it's not essential (structural dependencies as a whole is a rarity).
public sealed class HideChildOptionally : IExpressionFactory
{
    [Dependency(Structural = true)]
    public Identifier<bool> HideVariable { get; }

    public HideChildOptionally(Identifier<bool> hideVariable)
    {
        HideVariable = hideVariable;
    }

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        bool hide = context.Variables.GetValue(HideVariable);
        if (hide)
            return Expression.Default(context.ExpectedExpressionType);
        return context.Expressions.Children[0];
    }
}

// Another example, which suffers from this even more.
public sealed class Choice : IExpressionFactory
{
    [Dependency(Structural = true)]
    public Identifier<int> IndexVariable { get; }

    public Choice(Identifier<int> indexVariable)
    {
        IndexVariable = indexVariable;
    }

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        int index = context.Variables.GetValue(IndexVariable);
        return context.Expressions.Children[index];
    }
}
