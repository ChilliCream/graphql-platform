using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

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
        ReadOnlySpan<CursorKey> keys,
        ReadOnlySpan<object?> cursor,
        bool forward)
    {
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

    /// <summary>
    /// Build the select expression for a batch paging expression that uses grouping.
    /// </summary>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <param name="keys">
    /// The key definitions that represent the cursor.
    /// </param>
    /// <param name="orderExpressions">
    /// The order expressions that are used to sort the dataset.
    /// </param>
    /// <param name="orderMethods">
    /// The order methods that are used to sort the dataset.
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
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// If the number of keys is less than one or
    /// the number of order expressions does not match the number of order methods.
    /// </exception>
    public static Expression<Func<IGrouping<TK, TV>, Group<TK, TV>>> BuildBatchSelectExpression<TK, TV>(
        PagingArguments arguments,
        ReadOnlySpan<CursorKey> keys,
        ReadOnlySpan<LambdaExpression> orderExpressions,
        ReadOnlySpan<string> orderMethods,
        bool forward,
        ref int requestedCount)
    {
        if (keys.Length == 0)
        {
            throw new ArgumentException(
                "At least one key must be specified.",
                nameof(keys));
        }

        if (orderExpressions.Length != orderMethods.Length)
        {
            throw new ArgumentException(
                "The number of order expressions must match the number of order methods.",
                nameof(orderExpressions));
        }

        var group = Expression.Parameter(typeof(IGrouping<TK, TV>), "g");
        var groupKey = Expression.Property(group, "Key");
        Expression source = group;

        for (var i = 0; i < orderExpressions.Length; i++)
        {
            var methodName = orderMethods[i];
            var orderExpression = orderExpressions[i];

            if (!forward)
            {
                methodName = ReverseOrder(methodName);
            }

            var delegateType = typeof(Func<,>).MakeGenericType(typeof(TV), orderExpression.Body.Type);
            var typedOrderExpression = Expression.Lambda(delegateType, orderExpression.Body, orderExpression.Parameters);

            var method = GetEnumerableMethod(methodName, typeof(TV), typedOrderExpression);

            source = Expression.Call(
                method,
                source,
                typedOrderExpression);
        }

        if (arguments.After is not null)
        {
            var cursor = CursorParser.Parse(arguments.After, keys);
            source = BuildBatchWhereExpression<TV>(source, keys, cursor, forward);
        }

        if (arguments.Before is not null)
        {
            var cursor = CursorParser.Parse(arguments.Before, keys);
            source = BuildBatchWhereExpression<TV>(source, keys, cursor, forward);
        }

        if (arguments.First is not null)
        {
            source = Expression.Call(
                typeof(Enumerable),
                "Take",
                [typeof(TV)],
                source,
                Expression.Constant(arguments.First.Value + 1));
            requestedCount = arguments.First.Value;
        }

        if (arguments.Last is not null)
        {
            source = Expression.Call(
                typeof(Enumerable),
                "Take",
                [typeof(TV)],
                source,
                Expression.Constant(arguments.Last.Value + 1));
            requestedCount = arguments.Last.Value;
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
        return Expression.Lambda<Func<IGrouping<TK, TV>, Group<TK, TV>>>(createGroup, group);

        static string ReverseOrder(string method)
            => method switch
            {
                nameof(Queryable.OrderBy) => nameof(Queryable.OrderByDescending),
                nameof(Queryable.OrderByDescending) => nameof(Queryable.OrderBy),
                nameof(Queryable.ThenBy) => nameof(Queryable.ThenByDescending),
                nameof(Queryable.ThenByDescending) => nameof(Queryable.ThenBy),
                _ => method
            };

        static MethodInfo GetEnumerableMethod(string methodName, Type elementType, LambdaExpression keySelector)
        {
            return typeof(Enumerable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(elementType, keySelector.Body.Type);
        }
    }

    private static MethodCallExpression BuildBatchWhereExpression<T>(
        Expression enumerable,
        ReadOnlySpan<CursorKey> keys,
        object?[] cursor,
        bool forward)
    {
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

        return Expression.Call(
            typeof(Enumerable),
            "Where",
            [typeof(T)],
            enumerable,
            Expression.Lambda<Func<T, bool>>(expression!, parameter));
    }

    /// <summary>
    /// Extracts and removes the orderBy and thenBy expressions from the given expression tree.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static OrderRewriterResult ExtractAndRemoveOrder(Expression expression)
    {
        if(expression is null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        var rewriter = new OrderByRemovalRewriter();
        var (result, orderExpressions, orderMethods) = rewriter.Rewrite(expression);
        return new OrderRewriterResult(result, orderExpressions, orderMethods);
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

    public class Group<TKey, TValue>
    {
        public TKey Key { get; set; } = default!;

        public List<TValue> Items { get; set; } = default!;
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
        private readonly List<LambdaExpression> _orderExpressions = new();
        private readonly List<string> _orderMethods = new();
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
                var previousState = _insideSelectProjection;
                _insideSelectProjection = true;
                var result = base.VisitMethodCall(node);
                _insideSelectProjection = previousState;
                return result;
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
}
