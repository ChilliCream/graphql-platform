using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using GreenDonut.Data.Cursors;

namespace GreenDonut.Data.Expressions;

/// <summary>
/// This class provides helper methods to build slicing where clauses.
/// </summary>
internal static class ExpressionHelpers
{
    private static readonly MethodInfo s_createAndConvert = typeof(ExpressionHelpers)
        .GetMethod(nameof(CreateAndConvertParameter), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly ConcurrentDictionary<Type, Func<object?, Expression>> s_cachedConverters = new();

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
            cursorExpr[i] = CreateParameter(cursor.Values[i], keys[i].CompareMethod.Type);
        }

        Expression? expression = null;

        var parameter = Expression.Parameter(typeof(T), "t");
        var zero = Expression.Constant(0);

        for (var i = keys.Length - 1; i >= 0; i--)
        {
            var key = keys[i];

            var greaterThan = forward
                ? key.Direction == CursorKeyDirection.Ascending
                : key.Direction == CursorKeyDirection.Descending;

            Expression keyExpr = ReplaceParameter(key.Expression, parameter);

            if (key.IsNullable)
            {
                if (expression is null)
                {
                    throw new ArgumentException("The last key must be non-nullable.", nameof(keys));
                }

                // To avoid skipping any rows, NULL values are significant for the primary sorting condition.
                // For all secondary sorting conditions, NULL values are treated as last,
                // ensuring consistent behavior across different databases.
                if (i == 0 && cursor.NullsFirst)
                {
                    expression = BuildNullsFirstExpression(expression!, cursor.Values[i], keyExpr, cursorExpr[i], greaterThan, key.CompareMethod.MethodInfo);
                }
                else
                {
                    expression = BuildNullsLastExpression(expression!, cursor.Values[i], keyExpr, cursorExpr[i], greaterThan, key.CompareMethod.MethodInfo);
                }
            }
            else
            {
                expression = BuildNonNullExpression(expression, cursor.Values[i], keyExpr, cursorExpr[i], greaterThan, key.CompareMethod.MethodInfo);
            }
        }

        return Expression.Lambda<Func<T, bool>>(expression!, parameter);

        static Expression BuildNullsFirstExpression(
            Expression previousExpr,
            object? keyValue,
            Expression keyExpr,
            Expression cursorExpr,
            bool greaterThan,
            MethodInfo compareMethod)
        {
            Expression mainKeyExpr, secondaryKeyExpr;

            var zero = Expression.Constant(0);
            var nullConstant = Expression.Constant(null, keyExpr.Type);

            if (keyValue is null)
            {
                if (greaterThan)
                {
                    mainKeyExpr = Expression.Equal(keyExpr, nullConstant);

                    secondaryKeyExpr = Expression.NotEqual(keyExpr, nullConstant);

                    return Expression.OrElse(secondaryKeyExpr, Expression.AndAlso(mainKeyExpr, previousExpr));
                }
                else
                {
                    mainKeyExpr = Expression.Equal(keyExpr, nullConstant);

                    return Expression.AndAlso(mainKeyExpr, previousExpr);
                }
            }
            else
            {
                var nonNullKeyExpr = Expression.Property(keyExpr, "Value");
                var isNullExpression = Expression.Equal(keyExpr, nullConstant);

                mainKeyExpr = greaterThan
                    ? Expression.GreaterThan(
                        Expression.Call(nonNullKeyExpr, compareMethod, cursorExpr),
                        zero)
                    : Expression.OrElse(
                        Expression.LessThan(
                            Expression.Call(nonNullKeyExpr, compareMethod, cursorExpr),
                            zero), isNullExpression);

                secondaryKeyExpr = greaterThan
                    ? Expression.GreaterThanOrEqual(
                        Expression.Call(nonNullKeyExpr, compareMethod, cursorExpr),
                        zero)
                    : Expression.OrElse(
                        Expression.LessThanOrEqual(
                            Expression.Call(nonNullKeyExpr, compareMethod, cursorExpr),
                            zero), isNullExpression);

                return Expression.AndAlso(secondaryKeyExpr, Expression.OrElse(mainKeyExpr, previousExpr));
            }
        }

        static Expression BuildNullsLastExpression(
            Expression previousExpr,
            object? keyValue,
            Expression keyExpr,
            Expression cursorExpr,
            bool greaterThan,
            MethodInfo compareMethod)
        {
            Expression mainKeyExpr, secondaryKeyExpr;

            var zero = Expression.Constant(0);
            var nullConstant = Expression.Constant(null, keyExpr.Type);

            if (keyValue is null)
            {
                if (greaterThan)
                {
                    mainKeyExpr = Expression.Equal(keyExpr, nullConstant);

                    return Expression.AndAlso(mainKeyExpr, previousExpr);
                }
                else
                {
                    mainKeyExpr = Expression.Equal(keyExpr, nullConstant);

                    secondaryKeyExpr = Expression.NotEqual(keyExpr, nullConstant);

                    return Expression.OrElse(secondaryKeyExpr, Expression.AndAlso(mainKeyExpr, previousExpr));
                }
            }
            else
            {
                var nonNullKeyExpr = Expression.Property(keyExpr, "Value");
                var isNullExpression = Expression.Equal(keyExpr, nullConstant);

                mainKeyExpr = greaterThan
                    ? Expression.OrElse(
                        Expression.GreaterThan(
                            Expression.Call(nonNullKeyExpr, compareMethod, cursorExpr),
                            zero), isNullExpression)
                    : Expression.LessThan(
                            Expression.Call(nonNullKeyExpr, compareMethod, cursorExpr),
                            zero);

                secondaryKeyExpr = greaterThan
                    ? Expression.OrElse(
                        Expression.GreaterThanOrEqual(
                            Expression.Call(nonNullKeyExpr, compareMethod, cursorExpr),
                            zero), isNullExpression)
                    : Expression.LessThanOrEqual(
                            Expression.Call(nonNullKeyExpr, compareMethod, cursorExpr),
                            zero);

                return Expression.AndAlso(secondaryKeyExpr, Expression.OrElse(mainKeyExpr, previousExpr));
            }
        }

        static Expression BuildNonNullExpression(
            Expression? previousExpr,
            object? keyValue,
            Expression keyExpr,
            Expression cursorExpr,
            bool greaterThan,
            MethodInfo compareMethod)
        {
            var zero = Expression.Constant(0);
            Expression mainKeyExpr, secondaryKeyExpr;

            mainKeyExpr = greaterThan
                ? Expression.GreaterThan(
                   Expression.Call(keyExpr, compareMethod, cursorExpr),
                   zero)
                : Expression.LessThan(
                   Expression.Call(keyExpr, compareMethod, cursorExpr),
                   zero);

            secondaryKeyExpr = greaterThan
                ? Expression.GreaterThanOrEqual(
                    Expression.Call(keyExpr, compareMethod, cursorExpr),
                    zero)
                : Expression.LessThanOrEqual(
                    Expression.Call(keyExpr, compareMethod, cursorExpr),
                    zero);

            return previousExpr is null ? mainKeyExpr :
                Expression.AndAlso(secondaryKeyExpr, Expression.OrElse(mainKeyExpr, previousExpr));
        }
    }

    /// <summary>
    /// Build the select expression for a batch paging expression that uses grouping.
    /// </summary>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <param name="keys">
    /// The key definitions that represent the cursor.
    /// </param>
    /// <param name="forward">
    /// Defines how the dataset is sorted.
    /// </param>
    /// <param name="requestedCount">
    /// The number of items that are requested.
    /// </param>
    /// <typeparam name="TK">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TV">
    /// The value type.
    /// </typeparam>
    /// <exception cref="ArgumentException">
    /// If the number of keys is less than one or
    /// the number of order expressions does not match the number of order methods.
    /// </exception>
    public static BatchExpression<TK, TV> BuildBatchExpression<TK, TV>(
        PagingArguments arguments,
        ReadOnlySpan<CursorKey> keys,
        bool forward,
        ref int requestedCount)
    {
        if (keys.Length == 0)
        {
            throw new ArgumentException(
                "At least one key must be specified.",
                nameof(keys));
        }

        var group = Expression.Parameter(typeof(IGrouping<TK, TV>), "g");
        var groupKey = Expression.Property(group, "Key");
        Expression source = group;

        var offset = 0;
        var usesRelativeCursors = false;
        Cursor? cursor = null;

        if (arguments.After is not null)
        {
            cursor = CursorParser.Parse(arguments.After, keys);
            source = ApplyCursorPagination<TV>(source, keys, cursor, forward: true);
            offset = cursor.Offset ?? 0;

            if (cursor.IsRelative)
            {
                usesRelativeCursors = true;
            }
        }

        if (arguments.Before is not null)
        {
            if (usesRelativeCursors)
            {
                throw new ArgumentException(
                    "You cannot use `before` and `after` with relative cursors at the same time.",
                    nameof(arguments));
            }

            cursor = CursorParser.Parse(arguments.Before, keys);
            source = ApplyCursorPagination<TV>(source, keys, cursor, forward: false);
            offset = cursor.Offset ?? 0;
        }

        if (arguments.First is not null)
        {
            requestedCount = arguments.First.Value;
        }

        if (arguments.Last is not null)
        {
            requestedCount = arguments.Last.Value;
        }

        if (cursor?.IsRelative == true)
        {
            if ((arguments.Last is not null && cursor.Offset > 0) || (arguments.First is not null && cursor.Offset < 0))
            {
                throw new ArgumentException(
                    "Positive offsets are not allowed with `last`, and negative offsets are not allowed with `first`.",
                    nameof(arguments));
            }
        }

        offset = Math.Abs(offset);

        if (offset > 0)
        {
            source = Expression.Call(
                typeof(Enumerable),
                "Skip",
                [typeof(TV)],
                source,
                Expression.Constant(offset * requestedCount));
        }

        if (arguments.First is not null)
        {
            source = Expression.Call(
                typeof(Enumerable),
                "Take",
                [typeof(TV)],
                source,
                Expression.Constant(arguments.First.Value + 1));
        }

        if (arguments.Last is not null)
        {
            source = Expression.Call(
                typeof(Enumerable),
                "Take",
                [typeof(TV)],
                source,
                Expression.Constant(arguments.Last.Value + 1));
        }

        source = Expression.Call(
            typeof(Enumerable),
            "ToList",
            [typeof(TV)],
            source);

        var groupType = typeof(Group<TK, TV>);
        var bindings = new MemberBinding[]
        {
            Expression.Bind(groupType.GetProperty(nameof(Group<TK, TV>.Key))!, groupKey),
            Expression.Bind(groupType.GetProperty(nameof(Group<TK, TV>.Items))!, source)
        };

        var createGroup = Expression.MemberInit(Expression.New(groupType), bindings);
        return new BatchExpression<TK, TV>(
            Expression.Lambda<Func<IGrouping<TK, TV>, Group<TK, TV>>>(createGroup, group),
            arguments.Last is not null,
            cursor);
    }

    /// <summary>
    /// Extracts and removes the orderBy and thenBy expressions from the given expression tree.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static OrderRewriterResult ExtractAndRemoveOrder(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var rewriter = new OrderByRemovalRewriter();
        var (result, orderExpressions, orderMethods) = rewriter.Rewrite(expression);
        return new OrderRewriterResult(result, orderExpressions, orderMethods);
    }

    private static Expression CreateParameter(object? value, Type type)
    {
        var converter = s_cachedConverters.GetOrAdd(
            type,
            t =>
            {
                var method = s_createAndConvert.MakeGenericMethod(t);
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

    public static IQueryable<T> CursorPaginate<T>(
        this PrunedQuery<T> prunedQuery,
        CursorKey[] keys,
        Cursor cursor,
        bool forward)
    {
        var cursorPaginatedExpression = ApplyCursorPagination<T>(prunedQuery.Expression, keys, cursor, forward);
        return prunedQuery.Provider.CreateQuery<T>(cursorPaginatedExpression);
    }

    public static Expression ApplyCursorPagination<T>(
        Expression expression,
        ReadOnlySpan<CursorKey> keys,
        Cursor cursor,
        bool forward)
    {
        var whereExpr = BuildWhereExpression<T>(keys, cursor, forward);
        expression = Expression.Call(typeof(Enumerable), "Where", [typeof(T)], expression, whereExpr);
        return expression.ApplyCursorKeyOrdering<T>(keys, cursor.NullsFirst, forward);
    }

    public static Expression ApplyCursorKeyOrdering<T>(
        this Expression expression,
        ReadOnlySpan<CursorKey> keys,
        bool nullFirst,
        bool forward)
    {
        // TODO: This method is far from finished.
        // Should rebuild the order conditions based on the keys.

        if (keys.Length == 0)
        {
            return expression;
        }

        //static string ReverseOrder(string method) => method switch
        //{
        //    nameof(Queryable.OrderBy) => nameof(Queryable.OrderByDescending),
        //    nameof(Queryable.OrderByDescending) => nameof(Queryable.OrderBy),
        //    nameof(Queryable.ThenBy) => nameof(Queryable.ThenByDescending),
        //    nameof(Queryable.ThenByDescending) => nameof(Queryable.ThenBy),
        //    _ => method
        //};

        static MethodInfo GetEnumerableMethod(string methodName, Type elementType, LambdaExpression keySelector)
            => typeof(Enumerable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(elementType, keySelector.Body.Type);

        //for (var i = 0; i < orderExpressions.Length; i++)
        //{
        //    var methodName = forward ? orderMethods[i] : ReverseOrder(orderMethods[i]);
        //    var orderExpression = orderExpressions[i];
        //    var delegateType = typeof(Func<,>).MakeGenericType(typeof(TV), orderExpression.Body.Type);
        //    var typedOrderExpression =
        //        Expression.Lambda(delegateType, orderExpression.Body, orderExpression.Parameters);

        //    var method = GetEnumerableMethod(methodName, typeof(TV), typedOrderExpression);

        //    source = Expression.Call(
        //        method,
        //        source,
        //        typedOrderExpression);
        //}

        Expression? orderedExpression = null;
        var parameter = Expression.Parameter(typeof(T), "t");

        foreach (var key in keys)
        {
            var body = Expression.Invoke(key.Expression, parameter);

            if (key.IsNullable)
            {
                var nullCheck = Expression.Equal(body, Expression.Constant(null));
                var nullCheckLambda = Expression.Lambda(nullCheck, parameter);

                orderedExpression = orderedExpression == null
                    ? Expression.Call(typeof(Queryable), "OrderBy", [typeof(T), typeof(bool)], expression, nullCheckLambda)
                    : Expression.Call(typeof(Queryable), "ThenBy", [typeof(T), typeof(bool)], orderedExpression, nullCheckLambda);
            }

            var methodName = key.Direction == CursorKeyDirection.Ascending ? "OrderBy" : "OrderByDescending";

            var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), key.Expression.Body.Type);
            var typedOrderExpression = Expression.Lambda(delegateType, key.Expression.Body, key.Expression.Parameters);
            var method = GetEnumerableMethod(methodName, typeof(T), typedOrderExpression);

            orderedExpression = orderedExpression == null
                ? Expression.Call(method, expression, typedOrderExpression)
                : Expression.Call(method, orderedExpression, typedOrderExpression);
        }

        return orderedExpression ?? expression;
    }

    public class PrunedQuery<T>(Expression expression, IQueryProvider provider)
    {
        public Expression Expression { get; } = expression;

        public IQueryProvider Provider { get; } = provider;
    }

    private class ReplaceParameterVisitor(ParameterExpression parameter, Expression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == parameter ? replacement : base.VisitParameter(node);
        }
    }

    public class Group<TKey, TValue>
    {
        public TKey Key { get; set; } = default!;

        public List<TValue> Items { get; set; } = null!;
    }

    public readonly struct OrderRewriterResult(
        Expression expression,
        List<LambdaExpression> orderExpressions,
        List<string> orderMethods)
    {
        public Expression Expression => expression;

        public ReadOnlySpan<LambdaExpression> OrderExpressions => CollectionsMarshal.AsSpan(orderExpressions);

        public ReadOnlySpan<string> OrderMethods => CollectionsMarshal.AsSpan(orderMethods);
    }

    private sealed class OrderByRemovalRewriter : ExpressionVisitor
    {
        private readonly List<LambdaExpression> _orderExpressions = [];
        private readonly List<string> _orderMethods = [];
        private bool _insideSelectProjection;

        public (Expression, List<LambdaExpression>, List<string>) Rewrite(Expression expression)
        {
            var result = Visit(expression);

            _orderExpressions.Reverse();
            _orderMethods.Reverse();

            return (result, _orderExpressions, _orderMethods);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // we are not interested in nested order by calls
            if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == nameof(Queryable.Select))
            {
                // We first visit our parent. When we visit an expression
                // like "OrderBy().Select()", we want to visit the OrderBy first.
                var source = Visit(node.Arguments[0]);

                var previousState = _insideSelectProjection;
                _insideSelectProjection = true;
                var projection = Visit(node.Arguments[1]);
                _insideSelectProjection = previousState;

                return node.Update(null, [source, projection]);
            }

            if (node.Method.DeclaringType == typeof(Queryable)
                && (node.Method.Name == nameof(Queryable.OrderBy)
                    || node.Method.Name == nameof(Queryable.OrderByDescending)
                    || node.Method.Name == nameof(Queryable.ThenBy)
                    || node.Method.Name == nameof(Queryable.ThenByDescending)))
            {
                if (!_insideSelectProjection)
                {
                    var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
                    _orderExpressions.Add(lambda);
                    _orderMethods.Add(node.Method.Name);
                    return Visit(node.Arguments[0]);
                }
            }

            return base.VisitMethodCall(node);
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }

            return e;
        }
    }

    internal readonly struct BatchExpression<TK, TV>(
        Expression<Func<IGrouping<TK, TV>, Group<TK, TV>>> selectExpression,
        bool isBackward,
        Cursor? cursor)
    {
        public Expression<Func<IGrouping<TK, TV>, Group<TK, TV>>> SelectExpression { get; } = selectExpression;
        public bool IsBackward { get; } = isBackward;
        public Cursor? Cursor { get; } = cursor;
    }
}
