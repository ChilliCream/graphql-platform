using System;

namespace HotChocolate.Language.Visitors;

public class SyntaxVisitor : SyntaxVisitor<ISyntaxVisitorContext>
{
    public SyntaxVisitor(SyntaxVisitorOptions options = default)
        : base(options)
    {
    }

    public SyntaxVisitor(
        ISyntaxVisitorAction defaultResult,
        SyntaxVisitorOptions options = default)
        : base(defaultResult, options)
    {
    }

    public static ISyntaxVisitor<ISyntaxVisitorContext> Create(
        Func<ISyntaxNode, ISyntaxVisitorAction>? enter = null,
        Func<ISyntaxNode, ISyntaxVisitorAction>? leave = null,
        ISyntaxVisitorAction? defaultAction = null,
        SyntaxVisitorOptions options = default)
        => new DelegateSyntaxVisitor<ISyntaxVisitorContext>(
            enter is not null
                ? new VisitSyntaxNode<ISyntaxVisitorContext>((n, _) => enter(n))
                : null,
            leave is not null
                ? new VisitSyntaxNode<ISyntaxVisitorContext>((n, _) => leave(n))
                : null,
            defaultAction,
            options);

    public static ISyntaxVisitor<TContext> Create<TContext>(
        VisitSyntaxNode<TContext>? enter = null,
        VisitSyntaxNode<TContext>? leave = null,
        ISyntaxVisitorAction? defaultAction = null,
        SyntaxVisitorOptions options = default)
        where TContext : ISyntaxVisitorContext
    {
        defaultAction ??= Skip;

        enter ??= (_, _) => defaultAction;
        leave ??= (_, _) => defaultAction;

        return new DelegateSyntaxVisitor<TContext>(enter, leave, defaultAction, options);
    }

    public static ISyntaxVisitor<TContext> CreateWithNavigator<TContext>(
        VisitSyntaxNode<TContext>? enter = null,
        VisitSyntaxNode<TContext>? leave = null,
        ISyntaxVisitorAction? defaultAction = null,
        SyntaxVisitorOptions options = default)
        where TContext : INavigatorContext
    {
        defaultAction ??= Skip;

        VisitSyntaxNode<TContext> enterFunc = enter is not null
            ? (node, context) =>
            {
                context.Navigator.Push(node);
                return enter(node, context);
            }
            : (node, context) =>
            {
                context.Navigator.Push(node);
                return defaultAction;
            };

        VisitSyntaxNode<TContext> leaveFunc = leave is not null
            ? (node, context) =>
            {
                context.Navigator.Pop();
                return leave(node, context);
            }
            : (node, context) =>
            {
                context.Navigator.Pop();
                return defaultAction;
            };

        return new DelegateSyntaxVisitor<TContext>(enterFunc, leaveFunc, defaultAction, options);
    }
}
