namespace HotChocolate.Language.Visitors;

public delegate ISyntaxNode RewriteSyntaxNode<in TContext>(
    ISyntaxNode node,
    TContext context)
    where TContext : ISyntaxVisitorContext;
