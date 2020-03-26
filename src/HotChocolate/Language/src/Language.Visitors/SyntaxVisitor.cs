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

        public SyntaxVisitor()
        {
        }

        public SyntaxVisitor(ISyntaxVisitorAction defaultResult)
            : base(defaultResult)
        {
        }

        public static ISyntaxVisitor Create(
            Func<ISyntaxNode, ISyntaxVisitorAction>? enter = null,
            Func<ISyntaxNode, ISyntaxVisitorAction>? leave = null,
            ISyntaxVisitorAction? defaultAction = null)
        {
            return new DelegateSyntaxVisitor(
                enter is { }
                    ? new VisitSyntaxNode((n, c) => enter(n))
                    : null,
                leave is { }
                    ? new VisitSyntaxNode((n, c) => leave(n))
                    : null,
                default);
        }

        public static ISyntaxVisitor Create(
            VisitSyntaxNode? enter = null,
            VisitSyntaxNode? leave = null,
            ISyntaxVisitorAction? defaultAction = null)
        {
            return new DelegateSyntaxVisitor(enter, leave, default);
        }
    }
}
