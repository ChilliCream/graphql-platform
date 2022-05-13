using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Visitors;

public class SyntaxVisitorWithAncestors<TContext> : SyntaxVisitor<TContext>
    where TContext : ISyntaxVisitorContext
{
    private readonly List<ISyntaxNode> _ancestors = new();

    protected SyntaxVisitorWithAncestors(SyntaxVisitorOptions options)
        : base(options)
    {
    }

    protected virtual IEnumerable<T> GetAncestors<T>()
        where T : ISyntaxNode
    {
        // Start at second last node to skip self.
        for (var i = _ancestors.Count - 2; i >= 0; i--)
        {
            if (_ancestors[i] is not T typedAncestor)
            {
                continue;
            }

            yield return typedAncestor;
        }
    }

    protected virtual IEnumerable<T> GetAncestorsTopDown<T>()
        where T : ISyntaxNode
    {
        // Skip last node which is self.
        for (var i = 0; i < _ancestors.Count - 1; i++)
        {
            if (_ancestors[i] is not T typedAncestor)
            {
                continue;
            }

            yield return typedAncestor;
        }
    }

    protected virtual T? GetAncestor<T>()
        where T : ISyntaxNode
    {
        // Start at second last node to skip self.
        for (var i = _ancestors.Count - 1; i > 0; i--)
        {
            if (_ancestors[i] is not T typedAncestor)
            {
                continue;
            }

            return typedAncestor;
        }

        return default;
    }

    protected virtual ISyntaxNode? GetParent()
    {
        return _ancestors.Count > 1
            ? _ancestors[_ancestors.Count - 2]
            : default;
    }

    protected override ISyntaxVisitorAction Enter(ISyntaxNode node, TContext context)
    {
        _ancestors.Add(node);
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(ISyntaxNode node, TContext context)
    {
        _ancestors.RemoveAt(_ancestors.Count - 1);
        return base.Leave(node, context);
    }
}
