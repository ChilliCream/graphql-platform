using System.Collections.Immutable;
using System.Linq.Expressions;
using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

/// <summary>
/// A default implementation of the <see cref="IPredicateBuilder"/>.
/// </summary>
public sealed class DefaultPredicateBuilder : IPredicateBuilder
{
    private ImmutableArray<LambdaExpression> _predicates = [];

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultPredicateBuilder"/>.
    /// </summary>
    /// <param name="initialPredicate">
    /// The initial predicate to add.
    /// </param>
    public DefaultPredicateBuilder(LambdaExpression? initialPredicate)
    {
        if (initialPredicate is not null)
        {
            _predicates = _predicates.Add(initialPredicate);
        }
    }

    private DefaultPredicateBuilder(ImmutableArray<LambdaExpression> predicates)
    {
        _predicates = predicates;
    }

    /// <inheritdoc />
    public void Add<T>(Expression<Func<T, bool>> selector)
    {
        if (!_predicates.Contains(selector))
        {
            _predicates = _predicates.Add(selector);
        }
    }

    /// <inheritdoc />
    public Expression<Func<T, bool>>? TryCompile<T>()
    {
        if (_predicates.Length == 0)
        {
            return null;
        }

        if (_predicates.Length == 1)
        {
            return (Expression<Func<T, bool>>)_predicates[0];
        }

        if (_predicates.Length == 2)
        {
            return ExpressionHelpers.And(
                (Expression<Func<T, bool>>)_predicates[0],
                (Expression<Func<T, bool>>)_predicates[1]);
        }

        var expression = (Expression<Func<T, bool>>)_predicates[0];
        for (var i = 1; i < _predicates.Length; i++)
        {
            expression = ExpressionHelpers.And(
                expression,
                (Expression<Func<T, bool>>)_predicates[i]);
        }

        return expression;
    }

    /// <summary>
    /// Creates a new <see cref="DefaultPredicateBuilder"/> that branches off the current builder.
    /// </summary>
    /// <returns>
    /// Returns a new <see cref="DefaultPredicateBuilder"/>.
    /// </returns>
    public DefaultPredicateBuilder Branch()
        => new(_predicates);

    /// <summary>
    /// Gets an empty <see cref="DefaultPredicateBuilder"/>.
    /// </summary>
    public static DefaultPredicateBuilder Empty { get; } = new([]);
}
