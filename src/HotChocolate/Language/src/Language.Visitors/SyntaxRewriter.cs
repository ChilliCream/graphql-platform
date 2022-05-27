using System;
using System.Collections.Generic;

namespace HotChocolate.Language.Visitors;

public class SyntaxRewriter : SyntaxRewriter<ISyntaxVisitorContext>
{
    public static ISyntaxRewriter<ISyntaxVisitorContext> Create(
        Func<ISyntaxNode, ISyntaxNode> rewrite)
        => new DelegateSyntaxRewriter<ISyntaxVisitorContext>(
            rewrite: (node, _) => rewrite(node));

    public static ISyntaxRewriter<TContext> Create<TContext>(
        RewriteSyntaxNode<TContext>? rewrite = null,
        Func<ISyntaxNode, TContext, TContext>? enter = null,
        Action<ISyntaxNode, ISyntaxNode, TContext>? leave = null)
        where TContext : ISyntaxVisitorContext
        => new DelegateSyntaxRewriter<TContext>(rewrite, enter, leave);

    public static ISyntaxRewriter<TContext> CreateWithNavigator<TContext>(
        RewriteSyntaxNode<TContext>? rewrite = null,
        Func<ISyntaxNode, TContext, TContext>? enter = null,
        Action<ISyntaxNode, ISyntaxNode, TContext>? leave = null)
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

        Action<ISyntaxNode, ISyntaxNode, TContext> leaveFunc = leave is not null
            ? (node, rewrittenNode, context) =>
            {
                context.Navigator.Pop();
                leave(node, rewrittenNode, context);
            }
            : (_, _, context) =>
            {
                context.Navigator.Pop();
            };

        return new DelegateSyntaxRewriter<TContext>(rewrite, enterFunc, leaveFunc);
    }
}
