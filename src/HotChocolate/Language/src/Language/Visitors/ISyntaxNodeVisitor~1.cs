using System.Collections.Generic;
namespace HotChocolate.Language
{
    public interface ISyntaxNodeVisitor<T>
        : ISyntaxNodeVisitor
        where T : ISyntaxNode
    {
        /// <summary>
        /// Enter is called when the visitation method hit the node and
        /// is aboute to visit its subtree.
        /// </summary>
        /// <param name="node">
        /// The current node being visited.
        /// </param>
        /// <param name="parent">
        /// The parent immediately above this node, which may be an Array.
        /// </param>
        /// <param name="path">
        /// The index or key to this node from the parent node or Array.
        /// </param>
        /// <param name="ancestors">
        /// All nodes and Arrays visited before reaching parent of this node.
        /// These correspond to array indices in `path`.
        /// Note: ancestors includes arrays which contain the parent
        /// of visited node.
        /// </param>
        VisitorAction Enter(
            T node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors);

        /// <summary>
        /// Leave is called when the visitation method is about to leave
        /// this node the full subtree if this node was visited
        /// when this method is called.
        /// </summary>
        /// <param name="node">
        /// The current node being visited.
        /// </param>
        /// <param name="parent">
        /// The parent immediately above this node, which may be an Array.
        /// </param>
        /// <param name="path">
        /// The index or key to this node from the parent node or Array.
        /// </param>
        /// <param name="ancestors">
        /// All nodes and Arrays visited before reaching parent of this node.
        /// These correspond to array indices in `path`.
        /// Note: ancestors includes arrays which contain
        /// the parent of visited node.
        /// </param>
        VisitorAction Leave(
            T node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors);
    }
}
