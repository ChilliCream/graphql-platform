namespace HotChocolate.Data.ExpressionNodes;

internal readonly record struct SealedExpressionNode(
    SealedScope? Scope,
    IExpressionFactory ExpressionFactory,
    ReadOnlyStructuralDependencies Dependencies,
    Identifier[] Children)
{
    public bool IsInitialized => ExpressionFactory is not null;
}
