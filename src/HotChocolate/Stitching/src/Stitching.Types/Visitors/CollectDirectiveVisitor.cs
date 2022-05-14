using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Rewriters;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Visitors;

public abstract class CollectDirectiveVisitor<TContext, TDirective> : SyntaxVisitorWithAncestors<TContext>
    where TContext : ISyntaxVisitorContext
    where TDirective : ISyntaxNode
{
    private readonly List<SyntaxReference> _collectedDirectives = new();

    protected override ISyntaxVisitorAction DefaultAction => Continue;

    public IReadOnlyList<SyntaxReference> Directives => _collectedDirectives;

    public CollectDirectiveVisitor()
        : base(new SyntaxVisitorOptions
        {
            VisitDirectives = true
        })
    {
    }

    protected abstract bool ShouldCollect(ISyntaxNode directiveNode);

    protected abstract bool TryParseDirective(DirectiveNode directiveNode,
        [MaybeNullWhen(false)] out TDirective directive);

    protected override ISyntaxVisitorAction VisitChildren(DirectiveNode node, TContext context)
    {
        if (!ShouldCollect(node)
            || !TryParseDirective(node, out TDirective? directive))
        {
            return base.VisitChildren(node, context);
        }

        SyntaxReference syntaxReference = CreateSyntaxReference(directive);

        _collectedDirectives.Add(syntaxReference);
        return Skip;
    }

    private SyntaxReference CreateSyntaxReference(TDirective renameDirective)
    {
        IReadOnlyList<ISyntaxNode> ancestors = GetAncestors<ISyntaxNode>();
        return new SyntaxReference(ancestors, renameDirective);
    }
}
