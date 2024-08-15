using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Pagination.Expressions;

/// <summary>
/// This class provides helper methods to build slicing where clauses.
/// </summary>
public static class ExpressionHelpers
{
    private static readonly MethodInfo _createAndConvert = typeof(ExpressionHelpers)
        .GetMethod(nameof(CreateAndConvertParameter), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly ConcurrentDictionary<Type, Func<object?, Expression>> _cachedConverters = new();

    /// <summary>
    /// Builds a where expression that can be used to slice a dataset.
    /// </summary>
    /// <param name="keys">
    /// The key definitions that represent the cursor.
    /// </param>
    /// <param name="cursor">
    /// The key values that represent the cursor.
    /// </param>
    /// <param name="forward">
    /// Defines how the dataset is sorted.
    /// </param>
    /// <typeparam name="T">
    /// The entity type.
    /// </typeparam>
    /// <returns>
    /// Returns a where expression that can be used to slice a dataset.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// If <paramref name="keys"/> or <paramref name="cursor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// If the number of keys does not match the number of values.
    /// </exception>
    public static Expression<Func<T, bool>> BuildWhereExpression<T>(
        CursorKey[] keys,
        object?[] cursor,
        bool forward)
    {
        if (keys == null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        if (cursor == null)
        {
            throw new ArgumentNullException(nameof(cursor));
        }

        if (keys.Length == 0)
        {
            throw new ArgumentException("At least one key must be specified.", nameof(keys));
        }

        if (keys.Length != cursor.Length)
        {
            throw new ArgumentException("The number of keys must match the number of values.", nameof(cursor));
        }

        var cursorExpr = new Expression[cursor.Length];
        for (var i = 0; i < cursor.Length; i++)
        {
            cursorExpr[i] = CreateParameter(cursor[i], keys[i].Expression.ReturnType);
        }

        var handled = new List<CursorKey>();
        Expression? expression = null;

        var parameter = Expression.Parameter(typeof(T), "t");
        var zero = Expression.Constant(0);

        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            Expression? current = null;
            Expression keyExpr;

            for (var j = 0; j < handled.Count; j++)
            {
                var handledKey = handled[j];

                keyExpr =
                    Expression.Equal(
                        Expression.Call(
                            ReplaceParameter(handledKey.Expression, parameter),
                            handledKey.CompareMethod,
                            cursorExpr[j]),
                        zero);

                current = current is null
                    ? keyExpr
                    : Expression.AndAlso(current, keyExpr);
            }

            var greaterThan = forward
                ? key.Direction is CursorKeyDirection.Ascending
                : key.Direction is CursorKeyDirection.Descending;

            keyExpr =
                greaterThan
                    ? Expression.GreaterThan(
                        Expression.Call(
                            ReplaceParameter(key.Expression, parameter),
                            key.CompareMethod,
                            cursorExpr[i]),
                        zero)
                    : Expression.LessThan(
                        Expression.Call(
                            ReplaceParameter(key.Expression, parameter),
                            key.CompareMethod,
                            cursorExpr[i]),
                        zero);

            current = current is null
                ? keyExpr
                : Expression.AndAlso(current, keyExpr);
            expression = expression is null
                ? current
                : Expression.OrElse(expression, current);
            handled.Add(key);
        }

        return Expression.Lambda<Func<T, bool>>(expression!, parameter);
    }

    private static Expression CreateParameter(object? value, Type type)
    {
        var converter = _cachedConverters.GetOrAdd(
            type,
            t =>
            {
                var method = _createAndConvert.MakeGenericMethod(t);
                return v => (Expression)method.Invoke(null, [v])!;
            });

        return converter(value);
    }

    private static Expression CreateAndConvertParameter<T>(object value)
    {
        Expression<Func<T>> lambda = () => (T)value;
        return lambda.Body;
    }

    private static Expression ReplaceParameter(
        LambdaExpression expression,
        ParameterExpression replacement)
    {
        var visitor = new ReplaceParameterVisitor(expression.Parameters[0], replacement);
        return visitor.Visit(expression.Body);
    }

    private class ReplaceParameterVisitor(ParameterExpression parameter, Expression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == parameter ? replacement : base.VisitParameter(node);
        }
    }
}
