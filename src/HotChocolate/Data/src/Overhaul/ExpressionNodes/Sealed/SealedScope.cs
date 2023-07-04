namespace HotChocolate.Data.ExpressionNodes;

public sealed class SealedScope
{
    public SealedScope? ParentScope { get; }
    public SealedExpressionNode Root { get; }
    public SealedExpressionNode Instance { get; }

    public SealedScope(
        SealedExpressionNode root,
        SealedExpressionNode instance,
        SealedScope? parentScope)
    {
        Root = root;
        Instance = instance;
        ParentScope = parentScope;
    }
}
