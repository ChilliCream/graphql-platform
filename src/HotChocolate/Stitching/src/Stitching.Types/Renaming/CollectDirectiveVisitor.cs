using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Renaming;

public abstract class CollectDirectiveVisitor<TContext>
    : SyntaxVisitorWithAncestors<TContext>
    where TContext : ISyntaxVisitorContext
{
    private readonly List<SyntaxReference> _collectedDirectives = new();

    protected override ISyntaxVisitorAction DefaultAction => Continue;

    public IReadOnlyList<SyntaxReference> Directives => _collectedDirectives;

    protected CollectDirectiveVisitor()
        : base(new SyntaxVisitorOptions
        {
            VisitDirectives = true
        })
    {
    }

    protected abstract bool ShouldCollect(ISyntaxNode directiveNode, TContext context, out ISyntaxNode? syntaxNode);

    protected override ISyntaxVisitorAction VisitChildren(DirectiveNode node, TContext context)
    {
        if (!ShouldCollect(node, context, out ISyntaxNode? collectedNode))
        {
            return base.VisitChildren(node, context);
        }

        SyntaxReference syntaxReference = CreateSyntaxReference(collectedNode);

        _collectedDirectives.Add(syntaxReference);
        return Skip;
    }

    private SyntaxReference CreateSyntaxReference(ISyntaxNode? node)
    {
        return new SyntaxReference(Ancestors, node);
    }
}
