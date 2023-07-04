using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class SealedMetaTree
{
    public SealedExpressionNode[] Nodes { get; }
    public SealedExpressionNode Root { get; }
    public IReadOnlyDictionary<Identifier, SealedExpressionNode> SelectionIdToOuterNode { get; }

    public SealedMetaTree(
        SealedExpressionNode[] nodes,
        SealedExpressionNode root,
        IReadOnlyDictionary<Identifier, SealedExpressionNode> selectionIdToOuterNode)
    {
        Nodes = nodes;
        Root = root;
        SelectionIdToOuterNode = selectionIdToOuterNode;
    }

    public ref SealedExpressionNode NodeRef(Identifier id) => ref Nodes[id.Value - 1];
}
