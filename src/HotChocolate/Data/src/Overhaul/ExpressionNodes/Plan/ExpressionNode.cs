using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class ExpressionNode
{
    public ExpressionNode? Parent { get; set; }
    // This exists in order to be able to wrap the instance used in this scope
    // without changing all dependencies each time.
    public Scope? Scope { get; set; }
    public Identifier Id { get; set; }
    public required IExpressionFactory ExpressionFactory { get; set; }
    public StructuralDependencies? OwnDependencies { get; set; }
    public List<ExpressionNode>? Children { get; set; } = new();
    public ExpressionNode? InnermostInitialNode { get; set; }

    public ExpressionNode GetInnermostInitialNode() => InnermostInitialNode ?? this;
}
