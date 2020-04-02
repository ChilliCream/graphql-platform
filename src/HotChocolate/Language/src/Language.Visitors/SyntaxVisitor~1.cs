using System.Collections.Generic;

namespace HotChocolate.Language.Visitors
{
    public class SyntaxVisitor<TContext>
        : ISyntaxVisitor<TContext>
        where TContext : ISyntaxVisitorContext
    {
        private static readonly SyntaxNodeListPool _listPool = new SyntaxNodeListPool();

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
            List<ISyntaxNode> ancestors = _listPool.Get();

            try
            {
                return Visit(node, ancestors, context);
            }
            finally
            {
                _listPool.Return(ancestors);
            }
        }

        private ISyntaxVisitorAction Visit(
            ISyntaxNode node,
            List<ISyntaxNode> ancestors,
            TContext context)
        {
            ancestors.TryPeek(out ISyntaxNode? parent);
            var localContext = OnBeforeEnter(node, parent, ancestors, context);
            var result = Enter(node, localContext);
            localContext = OnAfterEnter(node, parent, ancestors, localContext);

            if (result.Kind == SyntaxVisitorActionKind.Break)
            {
                return result;
            }

            if (result.Kind == SyntaxVisitorActionKind.Continue)
            {
                ancestors.Push(node);
                foreach (ISyntaxNode child in GetNodes(node, localContext))
                {
                    result = Visit(child, ancestors, localContext);
                    if (result.Kind == SyntaxVisitorActionKind.Break)
                    {
                        return result;
                    }
                }
                ancestors.Pop();
            }

            localContext = OnBeforeLeave(node, parent, ancestors, localContext);
            result = Leave(node, localContext);
            localContext = OnAfterLeave(node, parent, ancestors, localContext);

            return result;
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
