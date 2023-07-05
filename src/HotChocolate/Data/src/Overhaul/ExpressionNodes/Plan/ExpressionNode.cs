using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

// This is deliberately made a class and is accessed by references.
// The plan meta tree is supposed to be super dynamic, and allowing references
// makes modifying it way easier.
// When we seal it for execution, the references turn into node ids.
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

    // TODO:
    // Might want to either specify with a bool that the node is innermost,
    // and have a property that stores the innermost node for all nodes except
    // the innermost node, which would store the outermost node.
    public ExpressionNode? InnermostInitialNode { get; set; }

    public ExpressionNode GetInnermostInitialNode() => InnermostInitialNode ?? this;

    public RelatedArrayNodesProxy AssumeArray() => new(this);
}

public readonly struct RelatedArrayNodesProxy
{
    private readonly ExpressionNode _node;

    public RelatedArrayNodesProxy(ExpressionNode node)
    {
        _node = node;
    }

    // This gives you the projected type, after the select into object array.
    public ExpressionNode MemberAccessLike => _node.Children![0];
    // If you're going to be adding filtering against the initial type, it must be done here.
    // (You should wrap this node with your filter node).
    public ExpressionNode InitialMemberAccess => _node.InnermostInitialNode!;

    public ExpressionNode Lambda => _node.Children![1];

    internal void ArrangeChildren(ExpressionNode memberAccess, ExpressionNode lambda)
    {
        if (_node.Children is { } list)
            list.Clear();
        else
            list = new();

        _node.InnermostInitialNode = memberAccess;
        memberAccess.Parent = _node;
        lambda.Parent = _node;
        list.Add(memberAccess);
        list.Add(lambda);
    }
}
