using System.Collections.ObjectModel;

namespace HotChocolate.Data.ExpressionNodes;

public class PlanMetaTree
{
    public ReadOnlyDictionary<Identifier, ExpressionNode> SelectionIdToOuterNode { get; }
    public ExpressionNode Root { get; }

    public PlanMetaTree(
        ReadOnlyDictionary<Identifier, ExpressionNode> selectionIdToOuterNode,
        ExpressionNode root)
    {
        SelectionIdToOuterNode = selectionIdToOuterNode;
        Root = root;
    }
}
