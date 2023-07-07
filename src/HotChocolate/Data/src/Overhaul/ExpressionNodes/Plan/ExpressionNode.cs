using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HotChocolate.Data.ExpressionNodes;

// This is deliberately made a class and is accessed by references.
// The plan meta tree is supposed to be super dynamic, and allowing references
// makes modifying it way easier.
// When we seal it for execution, the references turn into node ids.
public sealed class ExpressionNode
{
    public ExpressionNode(IExpressionFactory factory)
    {
        ExpressionFactory = factory;
        Children = new ChildrenCollection(this);
    }

    // Even though the parent here is not used, we might need it later.
    public ExpressionNode? Parent { get; internal set; }

    // This exists in order to be able to wrap the instance used in this scope
    // without changing all dependencies each time.
    public Scope? Scope { get; set; }

    // Initialized when the tree is sealed.
    internal Identifier Id { get; set; }

    // This will likely require some more care.
    // TODO: We don't actually need this everywhere, maybe this should be stored in factories?
    public Type? ExpectedType { get; set; }

    public IExpressionFactory ExpressionFactory { get; set; }
    public Dependencies OwnDependencies { get; set; }
    internal List<ExpressionNode>? ChildrenInternal { get; set; }
    public ChildrenCollection Children { get; }

    // Stores the innermost node for nodes other than the innermost node.
    // The innermost node stores the outermost node here.
    private ExpressionNode? InnermostOrOutermostNode { get; set; }
    public bool IsInnermost { get; set; }

    public ExpressionNode InnermostInitialNode
    {
        get => IsInnermost ? this : InnermostOrOutermostNode!;
        set
        {
            Debug.Assert(!IsInnermost);
            InnermostOrOutermostNode = value;
        }
    }

    public ExpressionNode OutermostNode
    {
        get => InnermostInitialNode.InnermostOrOutermostNode!;
        set
        {
            Debug.Assert(IsInnermost);
            InnermostOrOutermostNode = value;
        }
    }

    public RelatedArrayNodesProxy AssumeArray() => new(this);
}


public class ChildrenCollection : IList<ExpressionNode>
{
    private readonly ExpressionNode _node;

    public ChildrenCollection(ExpressionNode node)
    {
        _node = node;
    }

    private List<ExpressionNode>? MaybeList => _node.ChildrenInternal;
    private List<ExpressionNode> List => _node.ChildrenInternal ??= new();

    public void Clear()
    {
        if (MaybeList is { } list)
        {
            if (list.Count > 0)
                throw new InvalidOperationException("This might leave dangling references");

            // foreach (var node in list)
            //     node.Parent = null;

            list.Clear();
        }
    }

    public bool Contains(ExpressionNode item) => MaybeList?.Contains(item) ?? false;
    public void CopyTo(ExpressionNode[] array, int arrayIndex) => MaybeList?.CopyTo(array, arrayIndex);
    public bool Remove(ExpressionNode item)
    {
        if (MaybeList is { } list)
        {
            if (list.Count > 0)
                throw new InvalidOperationException("This might leave dangling references");
        }

        return false;
    }

    public int Count => MaybeList?.Count ?? 0;
    public bool IsReadOnly => false;
    public int IndexOf(ExpressionNode item) => MaybeList?.IndexOf(item) ?? -1;
    public void Insert(int index, ExpressionNode node)
    {
        SetSelfAsParent(node);
        List.Insert(index, node);
    }
    public void RemoveAt(int index) => MaybeList!.RemoveAt(index);
    public void Add(ExpressionNode node)
    {
        SetSelfAsParent(node);
        List.Add(node);
    }

    public ExpressionNode this[int index]
    {
        get => MaybeList![index];
        set => MaybeList![index] = value;
    }

    public IEnumerator<ExpressionNode> GetEnumerator()
        => MaybeList?.GetEnumerator() ?? Enumerable.Empty<ExpressionNode>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void SetSelfAsParent(ExpressionNode node)
    {
        if (node.Parent == _node)
        {
            // if (Contains(node))
            throw new InvalidOperationException("Cannot add the same node twice");
        }
        if (node.Parent is not null)
        {
            // Do we want to do that here magically?
            // node.Parent.Children.Remove(node);
            throw new InvalidOperationException("Reset the parent before adding this into a list");
        }

        node.Parent = _node;
    }

}

public readonly struct RelatedArrayNodesProxy
{
    private readonly ExpressionNode _node;

    public RelatedArrayNodesProxy(ExpressionNode node)
    {
        _node = node;
    }

    // This gives you the projected type, after the select into object array.
    public ExpressionNode MemberAccessLike => _node.Children[0];
    // If you're going to be adding filtering against the initial type, it must be done here.
    // (You should wrap this node with your filter node).
    public ExpressionNode InitialMemberAccess => _node.InnermostInitialNode;

    public ExpressionNode Lambda => _node.Children[1];

    internal void ArrangeChildren(ExpressionNode memberAccess, ExpressionNode lambda)
    {
        Debug.Assert(!_node.IsInnermost);
        var list = _node.Children;
        list.Clear();

        _node.InnermostInitialNode = memberAccess;
        list.Add(memberAccess);
        list.Add(lambda);
    }
}
