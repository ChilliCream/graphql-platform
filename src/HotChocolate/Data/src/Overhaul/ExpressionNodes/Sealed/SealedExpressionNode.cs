namespace HotChocolate.Data.ExpressionNodes;

internal readonly record struct SealedExpressionNode(
    SealedScope? Scope,
    IExpressionFactory ExpressionFactory,
    Dependencies Dependencies,
    Identifier[] Children)
{
    public bool IsInitialized => ExpressionFactory is not null;
}
