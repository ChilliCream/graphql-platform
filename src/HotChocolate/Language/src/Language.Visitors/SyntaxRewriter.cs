namespace HotChocolate.Language.Visitors;

/// <summary>
/// Represents a helper class to create a new syntax rewriter.
/// </summary>
public static class SyntaxRewriter
{
    public static ISyntaxRewriter<object?> Create(
        Func<ISyntaxNode, ISyntaxNode> rewrite)
        => new DelegateSyntaxRewriter<object?>(
            rewrite: (node, _) => rewrite(node));

    public static ISyntaxRewriter<TContext> Create<TContext>(
        RewriteSyntaxNode<TContext>? rewrite = null,
        Func<ISyntaxNode, TContext, TContext>? enter = null,
        Action<ISyntaxNode?, TContext>? leave = null)
        => new DelegateSyntaxRewriter<TContext>(rewrite, enter, leave);

    public static ISyntaxRewriter<NavigatorContext> CreateWithNavigator(
        RewriteSyntaxNode<NavigatorContext>? rewrite = null,
        Func<ISyntaxNode, NavigatorContext, NavigatorContext>? enter = null,
        Action<ISyntaxNode?, NavigatorContext>? leave = null)
        => CreateWithNavigator<NavigatorContext>(rewrite, enter, leave);

    public static ISyntaxRewriter<TContext> CreateWithNavigator<TContext>(
        RewriteSyntaxNode<TContext>? rewrite = null,
        Func<ISyntaxNode, TContext, TContext>? enter = null,
        Action<ISyntaxNode?, TContext>? leave = null)
        where TContext : INavigatorContext
    {
        Func<ISyntaxNode, TContext, TContext> enterFunc = enter is not null
            ? (node, context) =>
            {
                context.Navigator.Push(node);
                return enter(node, context);
            }
            : (node, context) =>
            {
                context.Navigator.Push(node);
                return context;
            };

        Action<ISyntaxNode?, TContext> leaveFunc = leave is not null
            ? (node, context) =>
            {
                context.Navigator.Pop();
                leave(node, context);
            }
            : (_, context) =>
            {
                context.Navigator.Pop();
            };

        return new DelegateSyntaxRewriter<TContext>(rewrite, enterFunc, leaveFunc);
    }
}
