using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using GreenDonut.Data.Cursors;

namespace GreenDonut.Data.Expressions;

/// <summary>
/// This class provides helper methods to build slicing where clauses.
/// </summary>
internal static class ExpressionHelpers
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
    public static (Expression<Func<T, bool>> WhereExpression, int Offset) BuildWhereExpression<T>(
        ReadOnlySpan<CursorKey> keys,
        Cursor cursor,
        bool forward)
    {
        if (keys.Length == 0)
        {
            throw new ArgumentException("At least one key must be specified.", nameof(keys));
        }

        if (keys.Length != cursor.Values.Length)
        {
            throw new ArgumentException("The number of keys must match the number of values.", nameof(cursor.Values));
        }

        var cursorExpr = new Expression[cursor.Values.Length];
        for (var i = 0; i < cursor.Values.Length; i++)
        {
            cursorExpr[i] = CreateParameter(cursor.Values[i], keys[i].Expression.ReturnType);
        }

        var handled = new List<CursorKey>();
        Expression? expression = null;

        var parameter = Expression.Parameter(typeof(T), "t");

        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            Expression? current = null;
            Expression keyExpr;

            // Handle previously processed keys (AND conditions)
            foreach (var handledKey in handled)
            {
                keyExpr = Expression.Equal(
                    ReplaceParameter(handledKey.Expression, parameter),
                    cursorExpr[handled.IndexOf(handledKey)]
                );

                current = current is null ? keyExpr : Expression.AndAlso(current, keyExpr);
            }

            // Determine the direction of the comparison (greater or less than)
            var greaterThan = forward
                ? key.Direction == CursorKeyDirection.Ascending
                : key.Direction == CursorKeyDirection.Descending;

            keyExpr = key.Expression.ReturnType switch
            {
                { } t when t == typeof(string) => BuildStringComparison(key, parameter, cursorExpr[i], greaterThan),
                { } t when t == typeof(bool) => BuildBooleanComparison(key, parameter, cursorExpr[i], greaterThan),
                { } t when t == typeof(DateTime) => throw new NotSupportedException(
                    "DateTime comparisons are not supported."),
                { } t when t == typeof(ulong) =>
                    throw new NotSupportedException("ulong comparisons are not supported."),
                { } t when t == typeof(ushort) => throw new NotSupportedException(
                    "ushort comparisons are not supported."),
                _ => greaterThan
                    ? Expression.GreaterThan(ReplaceParameter(key.Expression, parameter), cursorExpr[i])
                    : Expression.LessThan(ReplaceParameter(key.Expression, parameter), cursorExpr[i])
            };

            current = current is null ? keyExpr : Expression.AndAlso(current, keyExpr);
            expression = expression is null ? current : Expression.OrElse(expression, current);

            handled.Add(key);
        }

        return (Expression.Lambda<Func<T, bool>>(expression!, parameter), cursor.Offset ?? 0);
    }

    /// <summary>
    /// Helper method to build string comparison using string.Compare
    /// </summary>
    /// <param name="key"></param>
    /// <param name="parameter"></param>
    /// <param name="cursorValue"></param>
    /// <param name="greaterThan"></param>
    /// <returns></returns>
    private static Expression BuildStringComparison(CursorKey key, ParameterExpression parameter, Expression cursorValue, bool greaterThan)
    {
        var memberExpr = ReplaceParameter(key.Expression, parameter);
        var compareMethod = typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])!;

        // Call string.Compare(memberExpr, cursorValue)
        var compareCall = Expression.Call(null, compareMethod, memberExpr, cursorValue);

        return greaterThan
            ? Expression.GreaterThan(compareCall, Expression.Constant(0))
            : Expression.LessThan(compareCall, Expression.Constant(0));
    }

    /// <summary>
    /// Helper method to build boolean comparison
    /// </summary>
    /// <param name="key"></param>
    /// <param name="parameter"></param>
    /// <param name="cursorValue"></param>
    /// <param name="greaterThan"></param>
    /// <returns></returns>
    private static Expression BuildBooleanComparison(CursorKey key, ParameterExpression parameter, Expression cursorValue, bool greaterThan)
    {
        var memberExpr = ReplaceParameter(key.Expression, parameter);

        return Expression.Equal(memberExpr, cursorValue);
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

    private static Expression CreateAndConvertParameter<T>(T value)
    {
        Expression<Func<T>> lambda = () => value;
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
