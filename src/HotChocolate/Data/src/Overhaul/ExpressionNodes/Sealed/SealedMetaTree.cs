using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class SealedMetaTree
{
    // The nodes are ordered in the dependency order. Instance -> Children -> Self
    internal SealedExpressionNode[] Nodes { get; }
    // This is also why we can be sure the root node is always the last node.
    internal int RootNodeIndex => Nodes.Length - 1;
    internal ref SealedExpressionNode Root => ref Nodes[RootNodeIndex];

    internal IReadOnlyDictionary<Identifier, Identifier> SelectionIdToOuterNode { get; }

    internal SealedMetaTree(
        SealedExpressionNode[] nodes,
        IReadOnlyDictionary<Identifier, Identifier> selectionIdToOuterNode)
    {
        Nodes = nodes;
        SelectionIdToOuterNode = selectionIdToOuterNode;
    }

    internal ref SealedExpressionNode NodeRef(Identifier id)
        => ref Nodes[id.AsIndex()];
    internal ref SealedExpressionNode NodeRefBySelectionId(Identifier id)
        => ref NodeRef(SelectionIdToOuterNode[id]);
}
