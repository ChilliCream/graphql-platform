using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class SealedExpressionNode
{
    public SealedScope? Scope { get; }
    public Identifier Id { get; }
    public IExpressionFactory ExpressionFactory { get; }
    public ReadOnlyStructuralDependencies Dependencies { get; }
    public IReadOnlyList<Identifier> Children { get; }

    public SealedExpressionNode(
        SealedScope? scope,
        Identifier id,
        IExpressionFactory expressionFactory,
        ReadOnlyStructuralDependencies dependencies,
        IReadOnlyList<Identifier> children)
    {
        Scope = scope;
        Id = id;
        ExpressionFactory = expressionFactory;
        Dependencies = dependencies;
        Children = children;
    }
}
