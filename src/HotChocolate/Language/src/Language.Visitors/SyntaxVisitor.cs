using System;

namespace HotChocolate.Language.Visitors
{
    public delegate ISyntaxVisitorAction VisitSyntaxNode(
        ISyntaxNode node,
        ISyntaxVisitorContext context);

    public class SyntaxVisitor
        : SyntaxVisitor<ISyntaxVisitorContext>
        , ISyntaxVisitor
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

        public static ISyntaxVisitor Create(
            Func<ISyntaxNode, ISyntaxVisitorAction>? enter = null,
            Func<ISyntaxNode, ISyntaxVisitorAction>? leave = null,
            ISyntaxVisitorAction? defaultAction = null,
            SyntaxVisitorOptions options = default)
        {
            return new DelegateSyntaxVisitor(
                enter is { }
                    ? new VisitSyntaxNode((n, c) => enter(n))
                    : null,
                leave is { }
                    ? new VisitSyntaxNode((n, c) => leave(n))
                    : null,
                defaultAction,
                options);
        }

        public static ISyntaxVisitor Create(
            VisitSyntaxNode? enter = null,
            VisitSyntaxNode? leave = null,
            ISyntaxVisitorAction? defaultAction = null,
            SyntaxVisitorOptions options = default)
        {
            return new DelegateSyntaxVisitor(enter, leave, defaultAction, options);
        }
    }
}
