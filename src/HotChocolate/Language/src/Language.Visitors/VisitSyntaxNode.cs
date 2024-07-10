namespace HotChocolate.Language.Visitors;

public delegate ISyntaxVisitorAction VisitSyntaxNode<in TContext>(ISyntaxNode node, TContext context);
