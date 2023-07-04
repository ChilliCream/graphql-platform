using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class SealedMetaTree
{
    // The nodes are ordered in the dependency order. Instance -> Children -> Self
    public SealedExpressionNode[] Nodes { get; }
    // This is also why we can be sure the root node is always the last node.
    public SealedExpressionNode Root => Nodes[^1];

    public IReadOnlyDictionary<Identifier, SealedExpressionNode> SelectionIdToOuterNode { get; }

    public SealedMetaTree(
        SealedExpressionNode[] nodes,
        IReadOnlyDictionary<Identifier, SealedExpressionNode> selectionIdToOuterNode)
    {
        Nodes = nodes;
        SelectionIdToOuterNode = selectionIdToOuterNode;
    }

    public ref SealedExpressionNode NodeRef(Identifier id) => ref Nodes[id.AsIndex()];
}
