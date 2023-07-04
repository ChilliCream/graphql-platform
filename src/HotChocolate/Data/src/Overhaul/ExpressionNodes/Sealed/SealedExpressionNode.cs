using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class SealedExpressionNode
{
    public SealedExpressionNode? Parent { get; internal set; }
    public SealedScope? Scope { get; }
    public Identifier Id { get; }
    public IExpressionFactory ExpressionFactory { get; }
    public ReadOnlyStructuralDependencies Dependencies { get; }
    public IReadOnlyList<SealedExpressionNode> Children { get; }

    public SealedExpressionNode(
        SealedScope? scope,
        Identifier id,
        IExpressionFactory expressionFactory,
        ReadOnlyStructuralDependencies dependencies,
        IReadOnlyList<SealedExpressionNode> children)
    {
        Scope = scope;
        Id = id;
        ExpressionFactory = expressionFactory;
        Dependencies = dependencies;
        Children = children;
    }
}
