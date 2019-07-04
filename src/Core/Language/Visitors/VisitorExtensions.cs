using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public static partial class VisitorExtensions
    {
        private static readonly VisitationMap _defaultVisitationMap =
            new VisitationMap();

        public static void Accept(
            this ISyntaxNode node,
            ISyntaxNodeVisitor visitor) =>
            Accept(node, visitor, _defaultVisitationMap);

        public static void Accept(
            this ISyntaxNode node,
            ISyntaxNodeVisitor visitor,
            IVisitationMap visitationMap)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            if (visitationMap is null)
            {
                throw new ArgumentNullException(nameof(visitationMap));
            }

            var path = new ArrayStack<object>();
            var ancestors = new ArrayStack<SyntaxNodeInfo>();
            var ancestorNodes = new ArrayStack<ISyntaxNode>();
            var level = new ArrayStack<ArrayStack<SyntaxNodeInfo>>();

            var root = new ArrayStack<SyntaxNodeInfo>();
            root.Push(new SyntaxNodeInfo(node, null));
            level.Push(root);

            int index = 0;

            while (level.Count != 0)
            {
                bool isLeaving = level[index].Count == 0;
                VisitorAction action = default;
                SyntaxNodeInfo parent = default;
                SyntaxNodeInfo current = default;

                if (isLeaving)
                {
                    if (index == 0)
                    {
                        break;
                    }

                    level.Pop();
                    ancestorNodes.Pop();
                    current = ancestors.Pop();
                    parent = ancestors.Count == 0
                        ? default
                        : ancestors.Peek();

                    action = Leave(
                        visitor,
                        current.Node,
                        parent.Node,
                        path,
                        ancestorNodes);

                    if (current.Name != null)
                    {
                        path.Pop();
                    }

                    if (current.Index.HasValue)
                    {
                        path.Pop();
                    }

                    index--;
                }
                else
                {
                    current = level[index].Pop();

                    if (current.Name != null)
                    {
                        path.Push(current.Name);
                    }

                    if (current.Index.HasValue)
                    {
                        path.Push(current.Index.Value);
                    }

                    action = Enter(
                        visitor,
                        current.Node,
                        parent.Node,
                        path,
                        ancestorNodes);

                    if (action == VisitorAction.Continue)
                    {
                        level.Push(GetChildren(current.Node, visitationMap));
                    }
                    else if (action == VisitorAction.Skip)
                    {
                        // TODO : replace with empty
                        level.Push(new ArrayStack<SyntaxNodeInfo>());
                    }

                    parent = current;
                    ancestors.Push(current);
                    ancestorNodes.Push(current.Node);
                    index++;
                }

                if (action == VisitorAction.Break)
                {
                    break;
                }
            }

            level.Clear();
        }

        private static ArrayStack<SyntaxNodeInfo> GetChildren(
            ISyntaxNode node,
            IVisitationMap visitationMap)
        {
            var children = new ArrayStack<SyntaxNodeInfo>();
            visitationMap.ResolveChildren(node, children);
            return children;
        }

        private static VisitorAction Enter(
            ISyntaxNodeVisitor visitor,
            ISyntaxNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_enterVisitors.TryGetValue(node.GetType(), out IntVisitorFn v))
            {
                return v.Invoke(visitor, node, parent, path, ancestors);
            }
            return VisitorAction.Skip;
        }

        private static VisitorAction Leave(
            ISyntaxNodeVisitor visitor,
            ISyntaxNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_leaveVisitors.TryGetValue(node.GetType(), out IntVisitorFn v))
            {
                return v.Invoke(visitor, node, parent, path, ancestors);
            }
            return VisitorAction.Skip;
        }

        private delegate VisitorAction IntVisitorFn(
            ISyntaxNodeVisitor visitor,
            ISyntaxNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors);
    }
}
