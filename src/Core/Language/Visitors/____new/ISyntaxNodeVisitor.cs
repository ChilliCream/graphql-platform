using System.Runtime.InteropServices;
using System;
using System.ComponentModel;
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

    public interface ISyntaxNodeVisitor { }

    public static class VisitorExtensions
    {

        public static void Accept(
            this ISyntaxNode node,
            ISyntaxNodeVisitor visitor)
        {
            var path = new Stack<object>();
            var ancestors = new Stack<SyntaxNodeInfo>();
            var ancestorNodes = new Stack<ISyntaxNode>();
            var level = new List<Stack<SyntaxNodeInfo>>();
            // process.Add(new Stack<ISyntaxNode> { node });

            int index = -1;

            while (true)
            {
                index++;
                bool isLeaving = level[index].Count == 0;
                VisitorAction action = default;
                SyntaxNodeInfo parent = default;
                SyntaxNodeInfo current = default;

                if (isLeaving)
                {
                    path.Pop();
                    ancestorNodes.Pop();
                    current = ancestors.Pop();
                    parent = ancestors.Peek();
                    action = Leave(
                        visitor,
                        current.Node,
                        parent.Node,
                        path,
                        ancestorNodes);
                }
                else
                {
                    current = level[index].Pop();

                    action = Enter(
                        visitor,
                        current.Node,
                        parent.Node,
                        path,
                        ancestorNodes);

                    if (action == VisitorAction.Continue)
                    {
                        level.Add(GetChildren(current.Node));
                    }

                    parent = current;
                    ancestors.Push(current);
                    ancestorNodes.Push(current.Node);
                    path.Push((object)current.Index ?? current.Name);
                }

                if (action == VisitorAction.Break)
                {
                    break;
                }
            }

        }

        private static Stack<SyntaxNodeInfo> GetChildren(ISyntaxNode node)
        {
            throw new NotImplementedException();
        }

        private static VisitorAction Enter(ISyntaxNodeVisitor visitor, ISyntaxNode node, ISyntaxNode parent, IReadOnlyCollection<object> path, IReadOnlyCollection<ISyntaxNode> ancestors)
        { }

        private static VisitorAction Leave(ISyntaxNodeVisitor visitor, ISyntaxNode node, ISyntaxNode parent, IReadOnlyCollection<object> path, IReadOnlyCollection<ISyntaxNode> ancestors)
        { }

        private readonly struct SyntaxNodeInfo
        {
            public SyntaxNodeInfo(ISyntaxNode node, string name, int? index)
            {
                Node = node;
                Name = name;
                Index = index;
            }

            public ISyntaxNode Node { get; }
            public string Name { get; }
            public int? Index { get; }
        }
    }
}
