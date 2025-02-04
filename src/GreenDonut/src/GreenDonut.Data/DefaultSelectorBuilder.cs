using System.Collections.Immutable;
using System.Linq.Expressions;
using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

/// <summary>
/// A default implementation of the <see cref="ISelectorBuilder"/>.
/// </summary>
public sealed class DefaultSelectorBuilder : ISelectorBuilder
{
    private ImmutableArray<LambdaExpression> _selectors = ImmutableArray<LambdaExpression>.Empty;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultSelectorBuilder"/>.
    /// </summary>
    /// <param name="initialSelector">
    /// The initial selector to add.
    /// </param>
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

    /// <summary>
    /// Creates a new <see cref="DefaultSelectorBuilder"/> that branches off the current builder.
    /// </summary>
    /// <returns>
    /// Returns a new <see cref="DefaultSelectorBuilder"/>.
    /// </returns>
    public DefaultSelectorBuilder Branch()
        => new(_selectors);

    /// <summary>
    /// Gets an empty <see cref="DefaultSelectorBuilder"/>.
    /// </summary>
    public static DefaultSelectorBuilder Empty { get; } = new(ImmutableArray<LambdaExpression>.Empty);
}
