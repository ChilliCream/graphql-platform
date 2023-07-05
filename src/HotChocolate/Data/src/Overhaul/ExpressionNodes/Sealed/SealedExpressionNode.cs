using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public readonly record struct SealedExpressionNode(
    SealedScope? Scope,
    IExpressionFactory ExpressionFactory,
    ReadOnlyStructuralDependencies Dependencies,
    IReadOnlyList<Identifier> Children)
{
    public bool IsInitialized => ExpressionFactory is not null;
}
