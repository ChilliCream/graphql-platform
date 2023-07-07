using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class SealedMetaTree
{
    // The nodes are ordered in the dependency order. Instance -> Children -> Self
    internal SealedExpressionNode[] Nodes { get; }
    // This is also why we can be sure the root node is always the last node.
    internal int RootNodeIndex => Nodes.Length - 1;
    internal ref SealedExpressionNode Root => ref Nodes[RootNodeIndex];

    internal IReadOnlyDictionary<Identifier, int> SelectionIdToOuterNode { get; }

    internal SealedMetaTree(
        SealedExpressionNode[] nodes,
        IReadOnlyDictionary<Identifier, int> selectionIdToOuterNode)
    {
        Nodes = nodes;
        SelectionIdToOuterNode = selectionIdToOuterNode;
    }

    internal ref SealedExpressionNode NodeRef(int index)
        => ref Nodes[index];
    internal ref SealedExpressionNode NodeRefBySelectionId(Identifier index)
        => ref NodeRef(SelectionIdToOuterNode[index]);
}
