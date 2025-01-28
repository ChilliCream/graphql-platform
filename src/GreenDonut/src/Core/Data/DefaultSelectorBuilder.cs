using System.Collections.Immutable;
using System.Linq.Expressions;

namespace GreenDonut.Data;

/// <summary>
/// A default implementation of the <see cref="ISelectorBuilder"/>.
/// </summary>
public sealed class DefaultSelectorBuilder : ISelectorBuilder
{
    private ImmutableArray<LambdaExpression> _selectors = ImmutableArray<LambdaExpression>.Empty;

    public DefaultSelectorBuilder(LambdaExpression? initialSelector = null)
    {
        if (initialSelector is not null)
        {
            _selectors = _selectors.Add(initialSelector);
        }
    }

    private DefaultSelectorBuilder(ImmutableArray<LambdaExpression> selectors)
    {
        _selectors = selectors;
    }

    /// <inheritdoc />
    public void Add<T>(Expression<Func<T, T>> selector)
    {
        if (!_selectors.Contains(selector))
        {
            _selectors = _selectors.Add(selector);
        }
    }

    /// <inheritdoc />
    public Expression<Func<T, T>>? TryCompile<T>()
    {
        if (_selectors.Length == 0)
        {
            return null;
        }

        if (_selectors.Length == 1)
        {
            return (Expression<Func<T, T>>)_selectors[0];
        }

        if (_selectors.Length == 2)
        {
            return ExpressionHelpers.Combine(
                (Expression<Func<T, T>>)_selectors[0],
                (Expression<Func<T, T>>)_selectors[1]);
        }

        var expression = (Expression<Func<T, T>>)_selectors[0];
        for (var i = 1; i < _selectors.Length; i++)
        {
            expression = ExpressionHelpers.Combine(
                expression,
                (Expression<Func<T, T>>)_selectors[i]);
        }

        return expression;
    }

    public DefaultSelectorBuilder Branch()
        => new(_selectors);

    public static DefaultSelectorBuilder Empty { get; } = new(ImmutableArray<LambdaExpression>.Empty);
}
