using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GreenDonut.Projections;

/// <summary>
/// A default implementation of the <see cref="ISelectorBuilder"/>.
/// </summary>
/// <typeparam name="TValue"></typeparam>
[Experimental(Experiments.Projections)]
public sealed class DefaultSelectorBuilder<TValue> : ISelectorBuilder
{
    private List<LambdaExpression>? _selectors;

    /// <inheritdoc />
    public void Add<T>(Expression<Func<T, T>> selector)
    {
        if (typeof(T) != typeof(TValue))
        {
            throw new ArgumentException(
                "The projection type must match the DataLoader value type.",
                nameof(selector));
        }

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

        if (typeof(T) != typeof(TValue))
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
