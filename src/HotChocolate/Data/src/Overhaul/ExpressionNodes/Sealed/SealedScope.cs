namespace HotChocolate.Data.ExpressionNodes;

internal sealed record SealedScope(
    int InnermostInstance,
    int OutermostInstance,
    // NOTE: Having a reference to the parent scope here might be troublesome.
    //       This forbids us from getting expressions for sub-selections from the common tree
    //       because the nested expressions are allowed to reference stuff from the parent scope,
    //       which makes it so that we can't use it without having the parent scope.
    //       Ultimately, it might be fine to just fail to create an expression for a sub-selection
    //       in case this references was used.
    SealedScope? ParentScope)
{
}
