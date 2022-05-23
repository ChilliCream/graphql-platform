using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Renaming;

public class SyntaxVisitorWithAncestors<TContext> : SyntaxVisitor<TContext>
    where TContext : ISyntaxVisitorContext
{
    private readonly List<ISyntaxNode> _ancestors = new();

    protected IReadOnlyList<ISyntaxNode> Ancestors => _ancestors.AsReadOnly();

    protected SyntaxVisitorWithAncestors(SyntaxVisitorOptions syntaxVisitorOptions)
        : base(syntaxVisitorOptions)
    {
    }

    protected ISyntaxNode? Parent => Ancestors.Count > 0
        ? Ancestors[Ancestors.Count - 1]
        : default;

    protected override TContext OnBeforeEnter(ISyntaxNode node, ISyntaxNode? parent, TContext context)
    {
        if (parent != null)
        {
            _ancestors.Add(parent);
        }

        return base.OnBeforeEnter(node, parent, context);
    }

    protected override TContext OnAfterLeave(ISyntaxNode node, ISyntaxNode? parent, TContext context, ISyntaxVisitorAction action)
    {
        if (_ancestors.Count > 0)
        {
            _ancestors.RemoveAt(_ancestors.Count - 1);
        }

        return base.OnAfterLeave(node, parent, context, action);
    }
}