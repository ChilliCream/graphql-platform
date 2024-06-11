using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Filters;

public sealed class SyntaxFilterBuilder
{
    private readonly List<ISyntaxFilter> _filters = [];

    public SyntaxFilterBuilder Add(ISyntaxFilter filter)
    {
        if (!_filters.Contains(filter))
        {
            _filters.Add(filter);
        }
        return this;
    }

    public SyntaxFilterBuilder AddRange(IEnumerable<ISyntaxFilter> filters)
    {
        foreach (var filter in filters)
        {
            if (!_filters.Contains(filter))
            {
                _filters.Add(filter);
            }
        }
        return this;
    }

    public Func<SyntaxNode, bool> Build()
    {
        Func<SyntaxNode, bool> current = _ => false;

        for (var i = _filters.Count - 1; i >= 0; i--)
        {
            var filter = _filters[i];
            var parent = current;
            current = node => filter.IsMatch(node) || parent(node);
        }

        return current;
    }
}
