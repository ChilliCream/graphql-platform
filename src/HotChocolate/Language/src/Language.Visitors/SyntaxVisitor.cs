using System;
using System.Collections.Generic;

namespace HotChocolate.Language.Visitors
{
    public delegate ISyntaxVisitorAction VisitSyntaxNode(
        ISyntaxNode node,
        ISyntaxVisitorContext context);

    public class SyntaxVisitor
        : ISyntaxVisitor
    {
        private static readonly SyntaxNodeListPool _listPool = new SyntaxNodeListPool();
        private static readonly List<ISyntaxNode> _empty = new List<ISyntaxNode>();

        public SyntaxVisitor()
        {
            DefaultAction = Skip;
        }

        public SyntaxVisitor(ISyntaxVisitorAction defaultResult)
        {
            DefaultAction = defaultResult;
        }

        protected virtual ISyntaxVisitorAction DefaultAction { get; }

        public static ISyntaxVisitorAction Break { get; } = new BreakSyntaxVisitorAction();

        public static ISyntaxVisitorAction Skip { get; } = new SkipSyntaxVisitorAction();

        public static ISyntaxVisitorAction Continue { get; } = new ContinueSyntaxVisitorAction();

        public ISyntaxVisitorAction Visit(
            ISyntaxNode node,
            ISyntaxVisitorContext context)
        {
            var levels = new List<List<ISyntaxNode>>();
            List<ISyntaxNode> ancestors = _listPool.Get();
            List<ISyntaxNode> root = _listPool.Get();
            ISyntaxVisitorContext localContext = context;
            int index = 0;

            root.Push(node);
            levels.Push(root);

            ISyntaxNode? parent = null;
            ISyntaxVisitorAction result = DefaultAction;

            while (levels.Count > 0)
            {
                bool isLeaving = levels[index].Count == 0;
                ISyntaxNode? current;

                if (isLeaving)
                {
                    if (index == 0)
                    {
                        break;
                    }

                    _listPool.Return(levels.Pop());
                    current = ancestors.Pop();
                    ancestors.TryPeek(out parent);
                    result = Leave(current, localContext);
                    localContext = OnAfterLeave(current, parent, ancestors, localContext);
                    index--;
                }
                else
                {
                    current = levels[index].Pop();
                    localContext = OnBeforeEnter(current, parent, ancestors, localContext);
                    result = Enter(current, localContext);

                    if (result is IContinueSyntaxVisitorAction)
                    {
                        List<ISyntaxNode> nextLevel = _listPool.Get();
                        nextLevel.AddRange(GetNodes(current, localContext));
                        levels.Push(nextLevel);
                    }
                    else if (result is ISkipSyntaxVisitorAction)
                    {
                        levels.Push(_empty);
                    }

                    parent = current;
                    ancestors.Push(current);
                    index++;
                }

                if (result is IBreakSyntaxVisitorAction)
                {
                    break;
                }
            }

            if (levels.Count > 0)
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    _listPool.Return(levels[i]);
                }
            }

            _listPool.Return(ancestors);
            return result;
        }

        protected virtual ISyntaxVisitorAction Enter(
            ISyntaxNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ISyntaxNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual IEnumerable<ISyntaxNode> GetNodes(
            ISyntaxNode node,
            ISyntaxVisitorContext context) =>
            node.GetNodes();

        protected virtual ISyntaxVisitorContext OnBeforeEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            ISyntaxVisitorContext context) =>
            context;

        protected virtual ISyntaxVisitorContext OnAfterLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            ISyntaxVisitorContext context) =>
            context;

        public static ISyntaxVisitor Create(
            Func<ISyntaxNode, ISyntaxVisitorAction>? enter = null,
            Func<ISyntaxNode, ISyntaxVisitorAction>? leave = null,
            ISyntaxVisitorAction? defaultAction = null)
        {
            return new DelegateSyntaxVisitor(
                enter is { }
                    ? new VisitSyntaxNode((n, c) => enter(n))
                    : null,
                leave is { }
                    ? new VisitSyntaxNode((n, c) => leave(n))
                    : null,
                default);
        }

        public static ISyntaxVisitor Create(
            VisitSyntaxNode? enter = null,
            VisitSyntaxNode? leave = null,
            ISyntaxVisitorAction? defaultAction = null)
        {
            return new DelegateSyntaxVisitor(enter, leave, default);
        }
    }
}
