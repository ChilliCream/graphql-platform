namespace HotChocolate.Language.Visitors
{
    internal sealed class DelegateSyntaxVisitor
        : SyntaxVisitor
    {
        private readonly VisitSyntaxNode _enter;
        private readonly VisitSyntaxNode _leave;

        public DelegateSyntaxVisitor(
            VisitSyntaxNode? enter = null,
            VisitSyntaxNode? leave = null,
            ISyntaxVisitorAction? defaultResult = null,
            SyntaxVisitorOptions options = default)
            : base(defaultResult ?? Skip, options)
        {
            _enter = enter ?? new VisitSyntaxNode((n, c) => DefaultAction);
            _leave = leave ?? new VisitSyntaxNode((n, c) => DefaultAction);
        }

        protected override ISyntaxVisitorAction Enter(
            ISyntaxNode node,
            ISyntaxVisitorContext context) =>
            _enter(node, context);

        protected override ISyntaxVisitorAction Leave(
            ISyntaxNode node,
            ISyntaxVisitorContext context) =>
            _leave(node, context);
    }
}
