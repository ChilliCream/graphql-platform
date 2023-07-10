using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public class PlanMetaTree
{
    public IReadOnlyDictionary<Identifier, ExpressionNode> SelectionIdToInnerNode { get; }
    public ExpressionNode Root { get; }

    public PlanMetaTree(
        IReadOnlyDictionary<Identifier, ExpressionNode> selectionIdToInnerNode,
        ExpressionNode root)
    {
        SelectionIdToInnerNode = selectionIdToInnerNode;
        Root = root;
    }
}
