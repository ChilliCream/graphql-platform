namespace HotChocolate.Language.Visitors;

public delegate ISyntaxVisitorAction VisitSyntaxNode(
    ISyntaxNode node,
    ISyntaxVisitorContext context);

public delegate ISyntaxVisitorAction VisitSyntaxNode<in TContext>(
    ISyntaxNode node,
    TContext context)
    where TContext : ISyntaxVisitorContext;
