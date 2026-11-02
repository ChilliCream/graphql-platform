using Microsoft.CodeAnalysis;

namespace Mocha.Analyzers.Filters;

/// <summary>
/// Provides a builder for composing multiple <see cref="ISyntaxFilter"/> instances into a single
/// predicate that matches when any filter matches.
/// </summary>
public sealed class SyntaxFilterBuilder
{
    private readonly List<ISyntaxFilter> _filters = [];

    /// <summary>
    /// Adds a filter to the builder if it has not already been added.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    /// <returns>This builder instance for chaining.</returns>
    public SyntaxFilterBuilder Add(ISyntaxFilter filter)
    {
        if (!_filters.Contains(filter))
        {
            _filters.Add(filter);
        }

        return this;
    }

    /// <summary>
    /// Adds multiple filters to the builder, skipping any that have already been added.
    /// </summary>
    /// <param name="filters">The filters to add.</param>
    /// <returns>This builder instance for chaining.</returns>
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

    /// <summary>
    /// Builds a composite predicate that returns <see langword="true"/> if any registered filter matches
    /// the given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <returns>A predicate combining all registered filters with logical OR semantics.</returns>
    public Func<SyntaxNode, bool> Build()
    {
        var filters = _filters.ToArray();

        return node =>
        {
            for (var i = 0; i < filters.Length; i++)
            {
                if (filters[i].IsMatch(node))
                {
                    return true;
                }
            }

            return false;
        };
    }
}
