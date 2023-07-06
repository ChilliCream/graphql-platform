namespace HotChocolate.Data.ExpressionNodes;

internal readonly record struct SealedExpressionNode(
    SealedScope? Scope,
    IExpressionFactory ExpressionFactory,
    Dependencies Dependencies,
    Identifier[] Children,
    // The type that the expression should evaluate to.
    Type ExpectedType)
{
    public bool IsInitialized => ExpressionFactory is not null;
}
