namespace HotChocolate.Data.ExpressionNodes;

public sealed class Scope
{
    public Scope? ParentScope { get; set; }

    // This one can be wrapped
    public ExpressionNode Instance => InnerInstance!.OutermostNode;

    // This indicates the root node that gets you the instance expression.
    public ExpressionNode? InnerInstance { get; set; }

    public ExpressionNode? DeclaringNode { get; set; }
}
