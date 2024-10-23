using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GreenDonut.Predicates;

/// <summary>
/// A default implementation of the <see cref="IPredicateBuilder"/>.
/// </summary>
[Experimental(Experiments.Predicates)]
public sealed class DefaultPredicateBuilder : IPredicateBuilder
{
    private List<LambdaExpression>? _predicates;

    /// <inheritdoc />
    public void Add<T>(Expression<Func<T, bool>> selector)
    {
        _predicates ??= new List<LambdaExpression>();
        if (!_predicates.Contains(selector))
        {
            _predicates.Add(selector);
        }
    }

    /// <inheritdoc />
    public Expression<Func<T, bool>>? TryCompile<T>()
    {
        if (_predicates is null)
        {
            return null;
        }

        if (_predicates.Count == 1)
        {
            return (Expression<Func<T, bool>>)_predicates[0];
        }

        if (_predicates.Count == 2)
        {
            return ExpressionHelpers.And(
                (Expression<Func<T, bool>>)_predicates[0],
                (Expression<Func<T, bool>>)_predicates[1]);
        }

        var expression = (Expression<Func<T, bool>>)_predicates[0];
        for (var i = 1; i < _predicates.Count; i++)
        {
            expression = ExpressionHelpers.And(
                expression,
                (Expression<Func<T, bool>>)_predicates[i]);
        }

        return expression;
    }
}
