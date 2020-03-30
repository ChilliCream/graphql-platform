using System.Collections.Generic;

namespace HotChocolate.Language.Visitors
{
    public class SyntaxVisitor<TContext>
        : ISyntaxVisitor<TContext>
        where TContext : ISyntaxVisitorContext
    {
        private static readonly SyntaxNodeLevelPool _levelPool = new SyntaxNodeLevelPool();
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

        /// <summary>
        /// Ends traversing the graph.
        /// </summary>
        public static ISyntaxVisitorAction Break { get; } = new BreakSyntaxVisitorAction();

        /// <summary>
        /// Skips of the child nodes.
        /// </summary>
        public static ISyntaxVisitorAction Skip { get; } = new SkipSyntaxVisitorAction();

        /// <summary>
        /// Continues traversing the graph.
        /// </summary>
        public static ISyntaxVisitorAction Continue { get; } = new ContinueSyntaxVisitorAction();

        public ISyntaxVisitorAction Visit(
            ISyntaxNode node,
            TContext context)
        {
            List<List<ISyntaxNode>> levels = _levelPool.Get();
            List<ISyntaxNode> ancestors = _listPool.Get();
            List<ISyntaxNode> root = _listPool.Get();
            TContext localContext = context;
            int index = 0;

            root.Push(node);
            levels.Push(root);

            ISyntaxNode? parent = null;
            ISyntaxVisitorAction result = DefaultAction;

            try
            {
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
                        List<ISyntaxNode> lastLevel = levels.Pop();
                        if (lastLevel != _empty)
                        {
                            _listPool.Return(lastLevel);
                        }
                        current = ancestors.Pop();
                        ancestors.TryPeek(out parent);
                        localContext = OnBeforeLeave(current, parent, ancestors, localContext);
                        result = Leave(current, localContext);
                        localContext = OnAfterLeave(current, parent, ancestors, localContext);
                        index--;
                    }
                    else
                    {
                        current = levels[index].Pop();
                        localContext = OnBeforeEnter(current, parent, ancestors, localContext);
                        result = Enter(current, localContext);
                        localContext = OnAfterEnter(current, parent, ancestors, localContext);

                        if (result is IContinueSyntaxVisitorAction)
                        {
                            List<ISyntaxNode> nextLevel = _listPool.Get();
                            nextLevel.AddRange(GetNodes(current, localContext));
                            nextLevel.Reverse();
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

                return result;
            }
            finally
            {

                if (levels.Count > 0)
                {
                    for (int i = 0; i < levels.Count; i++)
                    {
                        _listPool.Return(levels[i]);
                    }
                }

                _listPool.Return(ancestors);
                _levelPool.Return(levels);
            }
        }

        protected virtual ISyntaxVisitorAction Enter(
            ISyntaxNode node,
            TContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ISyntaxNode node,
            TContext context) =>
            DefaultAction;

        protected virtual IEnumerable<ISyntaxNode> GetNodes(
            ISyntaxNode node,
            TContext context) =>
            node.GetNodes();

        protected virtual TContext OnBeforeEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            TContext context) =>
            context;

        protected virtual TContext OnAfterEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            TContext context) =>
            context;

        protected virtual TContext OnBeforeLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            TContext context) =>
            context;

        protected virtual TContext OnAfterLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            TContext context) =>
            context;
    }
}
