using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public class PlanMetaTree
{
    // Should be modified as the respective nodes get wrapped
    // (could also like the innermost nodes here instead and link the innermost nodes back to outermost nodes)
    // It doesn't really change the logic.
    public Dictionary<Identifier, ExpressionNode> SelectionIdToOuterNode { get; }
    public ExpressionNode Root { get; }

    public PlanMetaTree(
        Dictionary<Identifier, ExpressionNode> selectionIdToOuterNode,
        ExpressionNode root)
    {
        SelectionIdToOuterNode = selectionIdToOuterNode;
        Root = root;
    }
}
