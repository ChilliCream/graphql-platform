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
    private LambdaExpression? _expression;

    /// <inheritdoc />
    public void Add<T>(Expression<Func<T, T>> selector)
    {
        if (typeof(T) != typeof(TValue))
        {
            throw new ArgumentException(
                "The projection type must match the DataLoader value type.",
                nameof(selector));
        }

        if (_expression is null)
        {
            _expression = selector;
        }
        else
        {
            _expression = ExpressionHelpers.Combine(
                (Expression<Func<T, T>>)_expression,
                selector);
        }
    }

    /// <inheritdoc />
    public Expression<Func<T, T>>? TryCompile<T>()
    {
        if (_expression is null)
        {
            return null;
        }

        if (typeof(T) != typeof(TValue))
        {
            return null;
        }

        return (Expression<Func<T, T>>)_expression;
    }
}
