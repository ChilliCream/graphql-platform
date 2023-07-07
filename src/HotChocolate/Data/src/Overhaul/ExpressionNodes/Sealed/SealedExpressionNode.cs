using System;

namespace HotChocolate.Data.ExpressionNodes;

internal readonly record struct SealedExpressionNode(
    SealedScope? Scope,
    IExpressionFactory ExpressionFactory,
    Dependencies AllDependencies,

    // This is only needed for scoping the access to just the declared variables.
    Dependencies OwnDependencies,

    Identifier[] Children,
    // The type that the expression should evaluate to.
    Type ExpectedType)
{
    public bool IsInitialized => ExpressionFactory is not null;
}
