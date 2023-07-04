namespace HotChocolate.Data.ExpressionNodes;

public sealed class Scope
{
    public Scope? ParentScope { get; set; }

    // This indicates the root node that gets you the instance expression.
    public ExpressionNode? Root { get; set; }

    // This one can be wrapped
    public ExpressionNode? Instance { get; set; }
}
