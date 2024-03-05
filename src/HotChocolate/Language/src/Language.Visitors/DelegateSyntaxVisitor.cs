namespace HotChocolate.Language.Visitors;

internal sealed class DelegateSyntaxVisitor<TContext> : SyntaxVisitor<TContext>
{
    private readonly VisitSyntaxNode<TContext> _enter;
    private readonly VisitSyntaxNode<TContext> _leave;

    public DelegateSyntaxVisitor(
        VisitSyntaxNode<TContext>? enter = null,
        VisitSyntaxNode<TContext>? leave = null,
        ISyntaxVisitorAction? defaultResult = null,
        SyntaxVisitorOptions options = default)
        : base(defaultResult ?? Skip, options)
    {
        _enter = enter ?? ((_, _) => DefaultAction);
        _leave = leave ?? ((_, _) => DefaultAction);
    }

    protected override ISyntaxVisitorAction Enter(ISyntaxNode node, TContext context)
        => _enter(node, context);

    protected override ISyntaxVisitorAction Leave(ISyntaxNode node, TContext context)
        => _leave(node, context);
}
