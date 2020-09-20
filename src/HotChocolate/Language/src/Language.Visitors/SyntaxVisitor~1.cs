namespace HotChocolate.Language.Visitors
{
    public partial class SyntaxVisitor<TContext>
        : ISyntaxVisitor<TContext>
        where TContext : ISyntaxVisitorContext
    {
        private readonly SyntaxVisitorOptions _options;

        public SyntaxVisitor(SyntaxVisitorOptions options = default)
        {
            DefaultAction = Skip;
            _options = options;
        }

        public SyntaxVisitor(
            ISyntaxVisitorAction defaultResult,
            SyntaxVisitorOptions options = default)
        {
            DefaultAction = defaultResult;
            _options = options;
        }

        /// <summary>
        /// The visitor options.
        /// </summary>
        protected SyntaxVisitorOptions Options => _options;

        /// <summary>
        /// The visitor default action.
        /// </summary>
        /// <value></value>
        protected virtual ISyntaxVisitorAction DefaultAction { get; }

        /// <summary>
        /// Ends traversing the graph.
        /// </summary>
        public static ISyntaxVisitorAction Break { get; } = new BreakSyntaxVisitorAction();

        /// <summary>
        /// Skips the child nodes and the current node.
        /// </summary>
        public static ISyntaxVisitorAction Skip { get; } = new SkipSyntaxVisitorAction();

        /// <summary>
        /// Continues traversing the graph.
        /// </summary>
        public static ISyntaxVisitorAction Continue { get; } = new ContinueSyntaxVisitorAction();

        /// <summary>
        /// Skips the child node but completes the current node.
        /// </summary>
        public static ISyntaxVisitorAction SkipAndLeave { get; } =
            new SkipAndLeaveSyntaxVisitorAction();

        public ISyntaxVisitorAction Visit(
            ISyntaxNode node,
            TContext context) =>
            Visit<ISyntaxNode, ISyntaxNode?>(node, null, context);

        protected ISyntaxVisitorAction Visit<T, P>(
            T node,
            P parent,
            TContext context)
            where T : notnull, ISyntaxNode
            where P : ISyntaxNode?
        {
            var localContext = OnBeforeEnter(node, parent, context);
            var result = Enter(node, localContext);
            localContext = OnAfterEnter(node, parent, localContext, result);

            if (result.Kind == SyntaxVisitorActionKind.Continue)
            {
                if (VisitChildren(node, context).Kind == SyntaxVisitorActionKind.Break)
                {
                    return Break;
                }
            }

            if (result.Kind == SyntaxVisitorActionKind.Continue ||
                result.Kind == SyntaxVisitorActionKind.SkipAndLeave)
            {
                localContext = OnBeforeLeave(node, parent, localContext);
                result = Leave(node, localContext);
                OnAfterLeave(node, parent, localContext, result);
            }

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

        protected virtual TContext OnBeforeEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
            TContext context) =>
            context;

        protected virtual TContext OnAfterEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
            TContext context,
            ISyntaxVisitorAction action) =>
            context;

        protected virtual TContext OnBeforeLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            TContext context) =>
            context;

        protected virtual TContext OnAfterLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            TContext context,
            ISyntaxVisitorAction action) =>
            context;
    }
}
