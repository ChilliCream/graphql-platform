namespace HotChocolate.Data.ExpressionNodes;

internal sealed class SealedScope
{
    // NOTE: Having a reference to the parent scope here might be troublesome.
    //       This forbids us from getting expressions for sub-selections from the common tree
    //       because the nested expressions are allowed to reference stuff from the parent scope,
    //       which makes it so that we can't use it without having the parent scope.
    //       Ultimately, it might be fine to just fail to create an expression for a sub-selection
    //       in case this references was used.
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
