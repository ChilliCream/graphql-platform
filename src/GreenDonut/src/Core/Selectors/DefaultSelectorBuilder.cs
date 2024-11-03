using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GreenDonut.Selectors;

/// <summary>
/// A default implementation of the <see cref="ISelectorBuilder"/>.
/// </summary>
[Experimental(Experiments.Selectors)]
public sealed class DefaultSelectorBuilder : ISelectorBuilder
{
    private List<LambdaExpression>? _selectors;

    /// <inheritdoc />
    public void Add<T>(Expression<Func<T, T>> selector)
    {
        _selectors ??= new List<LambdaExpression>();
        if (!_selectors.Contains(selector))
        {
            _selectors.Add(selector);
        }
    }

    /// <inheritdoc />
    public Expression<Func<T, T>>? TryCompile<T>()
    {
        if (_selectors is null)
        {
            return null;
        }

        if (_selectors.Count == 1)
        {
            return (Expression<Func<T, T>>)_selectors[0];
        }

        if (_selectors.Count == 2)
        {
            return ExpressionHelpers.Combine(
                (Expression<Func<T, T>>)_selectors[0],
                (Expression<Func<T, T>>)_selectors[1]);
        }

        var expression = (Expression<Func<T, T>>)_selectors[0];
        for (var i = 1; i < _selectors.Count; i++)
        {
            expression = ExpressionHelpers.Combine(
                expression,
                (Expression<Func<T, T>>)_selectors[i]);
        }

        return expression;
    }
}
