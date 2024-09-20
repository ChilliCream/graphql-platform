namespace HotChocolate.Language.Visitors;

internal sealed class DelegateSyntaxRewriter<TContext> : SyntaxRewriter<TContext>
{
    private readonly RewriteSyntaxNode<TContext> _rewrite;
    private readonly Func<ISyntaxNode, TContext, TContext> _enter;
    private readonly Action<ISyntaxNode?, TContext> _leave;

    public DelegateSyntaxRewriter(
        RewriteSyntaxNode<TContext>? rewrite = null,
        Func<ISyntaxNode, TContext, TContext>? enter = null,
        Action<ISyntaxNode?, TContext>? leave = null)
    {
        _rewrite = rewrite ?? (static (node, _) => node);
        _enter = enter ?? (static (_, ctx) => ctx);
        _leave = leave ?? (static (_, _) => { });
    }

    protected override TContext OnEnter(
        ISyntaxNode node,
        TContext context)
        => _enter(node, context);

    protected override ISyntaxNode? OnRewrite(
        ISyntaxNode node,
        TContext context)
    {
        var rewrittenNode = base.OnRewrite(node, context);
        return rewrittenNode is null ? null : _rewrite(rewrittenNode, context);
    }

    protected override void OnLeave(
        ISyntaxNode? node,
        TContext context)
        => _leave(node, context);
}
