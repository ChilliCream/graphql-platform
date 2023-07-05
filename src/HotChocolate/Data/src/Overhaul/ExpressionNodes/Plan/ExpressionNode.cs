using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class ExpressionNode
{
    // Even though the parent here is not used, we might need it later.
    public ExpressionNode? Parent { get; set; }

    // This exists in order to be able to wrap the instance used in this scope
    // without changing all dependencies each time.
    public Scope? Scope { get; set; }

    // Initialized when the tree is sealed.
    internal Identifier Id { get; set; }

    public required IExpressionFactory ExpressionFactory { get; set; }
    public ReadOnlyStructuralDependencies? OwnDependencies { get; set; }
    public List<ExpressionNode>? Children { get; set; } = new();
    public ExpressionNode? InnermostInitialNode { get; set; }

    public ExpressionNode GetInnermostInitialNode() => InnermostInitialNode ?? this;
}
