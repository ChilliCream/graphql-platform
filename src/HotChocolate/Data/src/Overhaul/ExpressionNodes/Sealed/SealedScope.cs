namespace HotChocolate.Data.ExpressionNodes;

public sealed class SealedScope
{
    public SealedScope? ParentScope { get; }
    public SealedExpressionNode InnermostInstance { get; }
    public SealedExpressionNode OutermostInstance { get; }

    public SealedScope(
        SealedExpressionNode innermostInstance,
        SealedExpressionNode outermostInstance,
        SealedScope? parentScope)
    {
        InnermostInstance = innermostInstance;
        OutermostInstance = outermostInstance;
        ParentScope = parentScope;
    }
}
