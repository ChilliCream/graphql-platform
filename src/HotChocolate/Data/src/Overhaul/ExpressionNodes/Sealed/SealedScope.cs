namespace HotChocolate.Data.ExpressionNodes;

public sealed class SealedScope
{
    public SealedScope? ParentScope { get; }
    public Identifier InnermostInstance { get; }
    public Identifier OutermostInstance { get; }

    public SealedScope(
        Identifier innermostInstance,
        Identifier outermostInstance,
        SealedScope? parentScope)
    {
        InnermostInstance = innermostInstance;
        OutermostInstance = outermostInstance;
        ParentScope = parentScope;
    }
}
