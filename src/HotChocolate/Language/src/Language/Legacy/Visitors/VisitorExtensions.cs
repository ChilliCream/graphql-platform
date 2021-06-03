using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public static partial class VisitorExtensions
    {
        private static readonly VisitationMap _defaultVisitationMap = new();

        public static void Accept<T>(
            this ISyntaxNode node,
            VisitorFn<T> enter,
            VisitorFn<T> leave)
            where T : ISyntaxNode =>
            Accept(node, enter, leave, _defaultVisitationMap);

        public static void Accept<T>(
            this ISyntaxNode node,
            VisitorFn<T> enter,
            VisitorFn<T> leave,
            IVisitationMap visitationMap)
            where T : ISyntaxNode =>
            Accept(
                node,
                new VisitorFnWrapper<T>(enter, leave),
                visitationMap,
                null);

        public static void Accept<T>(
            this ISyntaxNode node,
            VisitorFn<T> enter,
            VisitorFn<T> leave,
            Func<ISyntaxNode, VisitorAction> defaultAction)
            where T : ISyntaxNode =>
            Accept(
                node,
                new VisitorFnWrapper<T>(enter, leave),
                _defaultVisitationMap,
                defaultAction);

        public static void Accept<T>(
            this ISyntaxNode node,
            VisitorFn<T> enter,
            VisitorFn<T> leave,
            IVisitationMap visitationMap,
            Func<ISyntaxNode, VisitorAction> defaultAction)
            where T : ISyntaxNode =>
            Accept(
                node,
                new VisitorFnWrapper<T>(enter, leave),
                visitationMap,
                defaultAction);

        public static void Accept(
            this ISyntaxNode node,
            ISyntaxNodeVisitor visitor) =>
            Accept(node, visitor, _defaultVisitationMap, null);

        public static void Accept(
            this ISyntaxNode node,
            ISyntaxNodeVisitor visitor,
            IVisitationMap visitationMap) =>
            Accept(node, visitor, visitationMap, null);

        public static void Accept(
            this ISyntaxNode node,
            ISyntaxNodeVisitor visitor,
            Func<ISyntaxNode, VisitorAction> defaultAction) =>
            Accept(node, visitor, _defaultVisitationMap, defaultAction);

        public static void Accept(
            this ISyntaxNode node,
            ISyntaxNodeVisitor visitor,
            IVisitationMap visitationMap,
            Func<ISyntaxNode, VisitorAction>? defaultAction)
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

            var path = new List<object>();
            var ancestors = new List<SyntaxNodeInfo>();
            var ancestorNodes = new List<ISyntaxNode>();
            var level = new List<List<SyntaxNodeInfo>>();

            var root = new List<SyntaxNodeInfo>();
            root.Push(new SyntaxNodeInfo(node, null));
            level.Push(root);

            var index = 0;
            SyntaxNodeInfo parent = default;

            while (level.Count != 0)
            {
                var isLeaving = level[index].Count == 0;
                SyntaxNodeInfo current;
                VisitorAction action;

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

                    if (action == VisitorAction.Default)
                    {
                        action = defaultAction?.Invoke(current.Node)
                            ?? VisitorAction.Skip;
                    }

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

                    if (action == VisitorAction.Default)
                    {
                        action = defaultAction?.Invoke(current.Node)
                            ?? VisitorAction.Skip;
                    }

                    if (action == VisitorAction.Continue)
                    {
                        level.Push(GetChildren(current.Node, visitationMap));
                    }
                    else if (action == VisitorAction.Skip)
                    {
                        level.Push(new List<SyntaxNodeInfo>());
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

        private static List<SyntaxNodeInfo> GetChildren(
            ISyntaxNode node,
            IVisitationMap visitationMap)
        {
            var children = new List<SyntaxNodeInfo>();
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
            if (_enterVisitors.TryGetValue(node.GetType(), out IntVisitorFn? v))
            {
                return v.Invoke(visitor, node, parent, path, ancestors);
            }
            return VisitorAction.Default;
        }

        private static VisitorAction Leave(
            ISyntaxNodeVisitor visitor,
            ISyntaxNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_leaveVisitors.TryGetValue(node.GetType(), out IntVisitorFn? v))
            {
                return v.Invoke(visitor, node, parent, path, ancestors);
            }
            return VisitorAction.Default;
        }

        private delegate VisitorAction IntVisitorFn(
            ISyntaxNodeVisitor visitor,
            ISyntaxNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors);
    }
}
