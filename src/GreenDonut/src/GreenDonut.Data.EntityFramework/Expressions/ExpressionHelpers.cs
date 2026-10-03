using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using GreenDonut.Data.Cursors;
using GreenDonut.Data.Internal;

namespace GreenDonut.Data.Expressions;

/// <summary>
/// This class provides helper methods to build slicing where clauses.
/// </summary>
internal static class ExpressionHelpers
{
    private static readonly MethodInfo s_createAndConvert = typeof(ExpressionHelpers)
        .GetMethod(nameof(CreateAndConvertParameter), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly ConcurrentDictionary<Type, Func<object?, Expression>> s_cachedConverters = new();
    private static readonly NullabilityInfoContext s_nullabilityInfoContext = new();
#if NET9_0_OR_GREATER
    private static readonly Lock s_nullabilityInfoContextLock = new();
#else
    private static readonly object s_nullabilityInfoContextLock = new();
#endif
    private static readonly Expression s_false = Expression.Constant(false);
    private static readonly Expression s_zero = Expression.Constant(0);

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
    /// <param name="nullOrdering">
    /// Defines the null ordering to be used.
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
        bool forward,
        NullOrdering nullOrdering)
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
            var parameterType = Nullable.GetUnderlyingType(keys[i].Expression.ReturnType)
                ?? keys[i].Expression.ReturnType;

            cursorExpr[i] = CreateParameter(cursor.Values[i], parameterType);
        }

        var handled = new List<CursorKey>();
        Expression? expression = null;

        var parameter = Expression.Parameter(typeof(T), "t");

        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            Expression? current = null;
            Expression keyExpr;

            for (var j = 0; j < handled.Count; j++)
            {
                var handledKey = handled[j];
                var handledKeyIsNullable = IsNullable(handledKey.Expression);

                keyExpr = BuildEqualToKeyExpr(
                    handledKey,
                    parameter,
                    cursor.Values[j],
                    handledKeyIsNullable,
                    cursorExpr[j]);

                current = current is null ? keyExpr : Expression.AndAlso(current, keyExpr);
            }

            var keyIsNullable = IsNullable(key.Expression);

            if (keyIsNullable && nullOrdering == NullOrdering.Unspecified)
            {
                throw new InvalidOperationException(
                    "The NullOrdering option must be specified in the paging options or "
                    + "arguments when using nullable keys.");
            }

            var greaterThan = forward
                ? key.Direction == CursorKeyDirection.Ascending
                : key.Direction == CursorKeyDirection.Descending;

            if (greaterThan)
            {
                keyExpr =
                    BuildGreaterThanKeyExpr(
                        key,
                        parameter,
                        cursor.Values[i],
                        keyIsNullable,
                        nullOrdering,
                        cursorExpr[i]);
            }
            else
            {
                keyExpr =
                    BuildLessThanKeyExpr(
                        key,
                        parameter,
                        cursor.Values[i],
                        keyIsNullable,
                        nullOrdering,
                        cursorExpr[i]);
            }

            current = current is null ? keyExpr : Expression.AndAlso(current, keyExpr);
            expression = expression is null ? current : Expression.OrElse(expression, current);
            handled.Add(key);
        }

        return (Expression.Lambda<Func<T, bool>>(expression!, parameter), cursor.Offset ?? 0);
    }

    private static Expression BuildEqualToKeyExpr(
        CursorKey cursorKey,
        ParameterExpression parameter,
        object? cursorValue,
        bool keyIsNullable,
        Expression cursorExpr)
    {
        var keyExpr = ReplaceParameter(cursorKey.Expression, parameter);
        keyExpr = LiftValueTypeIfNullable(keyExpr, keyIsNullable);

        // Access the value of the key if it is a nullable value type.
        var keyValueExpr = cursorKey.Expression.ReturnType.IsValueType && keyIsNullable
            ? Expression.Property(keyExpr, "Value")
            : keyExpr;

        if (keyIsNullable)
        {
            // A null constant typed to match keyExpr.Type.
            var nullConst = Expression.Constant(null, keyExpr.Type);

            if (cursorValue is null)
            {
                // SQL: WHERE key IS NULL.
                keyExpr = Expression.Equal(keyExpr, nullConst);
            }
            else
            {
                // SQL: WHERE key IS NOT NULL AND key = cursorValue.
                keyExpr = Expression.AndAlso(
                    Expression.NotEqual(keyExpr, nullConst),
                    BuildEqualComparison(cursorKey, keyValueExpr, cursorExpr));
            }
        }
        else
        {
            // SQL: WHERE key = cursorValue.
            keyExpr = BuildEqualComparison(cursorKey, keyExpr, cursorExpr);
        }

        return keyExpr;
    }

    private static Expression BuildGreaterThanKeyExpr(
        CursorKey cursorKey,
        ParameterExpression parameter,
        object? cursorValue,
        bool keyIsNullable,
        NullOrdering nullOrdering,
        Expression cursorExpr)
    {
        var keyExpr = ReplaceParameter(cursorKey.Expression, parameter);
        keyExpr = LiftValueTypeIfNullable(keyExpr, keyIsNullable);

        // Access the value of the key if it is a nullable value type.
        var keyValueExpr =
            cursorKey.Expression.ReturnType.IsValueType && keyIsNullable
                ? Expression.Property(keyExpr, "Value")
                : keyExpr;

        if (keyIsNullable)
        {
            // A null constant typed to match keyExpr.Type.
            var nullConst = Expression.Constant(null, keyExpr.Type);

            if (cursorValue is null)
            {
                keyExpr = nullOrdering == NullOrdering.NativeNullsFirst
                    // With nulls first, any non-null value is greater than null.
                    // SQL: WHERE key IS NOT NULL.
                    ? Expression.NotEqual(keyExpr, nullConst)
                    // With nulls last, no value is greater than null.
                    // SQL: WHERE false.
                    : s_false;
            }
            else
            {
                if (nullOrdering == NullOrdering.NativeNullsFirst)
                {
                    // SQL: WHERE key > cursorValue.
                    keyExpr = BuildGreaterThanComparison(cursorKey, keyValueExpr, cursorExpr);
                }
                else
                {
                    // When nulls are last, null is greater than any non-null value.
                    // SQL: WHERE key IS NULL OR key > cursorValue.
                    keyExpr = Expression.OrElse(
                        Expression.Equal(keyExpr, nullConst),
                        BuildGreaterThanComparison(cursorKey, keyValueExpr, cursorExpr));
                }
            }
        }
        else
        {
            // SQL: WHERE key > cursorValue.
            keyExpr = BuildGreaterThanComparison(cursorKey, keyExpr, cursorExpr);
        }

        return keyExpr;
    }

    private static Expression BuildLessThanKeyExpr(
        CursorKey cursorKey,
        ParameterExpression parameter,
        object? cursorValue,
        bool keyIsNullable,
        NullOrdering nullOrdering,
        Expression cursorExpr)
    {
        var keyExpr = ReplaceParameter(cursorKey.Expression, parameter);
        keyExpr = LiftValueTypeIfNullable(keyExpr, keyIsNullable);

        // Access the value of the key if it is a nullable value type.
        var keyValueExpr =
            cursorKey.Expression.ReturnType.IsValueType && keyIsNullable
                ? Expression.Property(keyExpr, "Value")
                : keyExpr;

        if (keyIsNullable)
        {
            // A null constant typed to match keyExpr.Type.
            var nullConst = Expression.Constant(null, keyExpr.Type);

            if (cursorValue is null)
            {
                keyExpr = nullOrdering == NullOrdering.NativeNullsFirst
                    // With nulls first, no value is less than null.
                    // SQL: WHERE false.
                    ? s_false
                    // With nulls last, any non-null value is less than null.
                    // SQL: WHERE key IS NOT NULL.
                    : Expression.NotEqual(keyExpr, nullConst);
            }
            else
            {
                if (nullOrdering == NullOrdering.NativeNullsFirst)
                {
                    // With nulls first, null is less than any non-null value.
                    // SQL: WHERE key IS NULL OR key < cursorValue.
                    keyExpr = Expression.OrElse(
                        Expression.Equal(keyExpr, nullConst),
                        BuildLessThanComparison(cursorKey, keyValueExpr, cursorExpr));
                }
                else
                {
                    // SQL: WHERE key < cursorValue.
                    keyExpr = BuildLessThanComparison(cursorKey, keyValueExpr, cursorExpr);
                }
            }
        }
        else
        {
            // SQL: WHERE key < cursorValue.
            keyExpr = BuildLessThanComparison(cursorKey, keyExpr, cursorExpr);
        }

        return keyExpr;
    }

    private static bool IsNullable(LambdaExpression expression)
    {
        // A nullable value-type return.
        if (expression.ReturnType.IsValueType
            && Nullable.GetUnderlyingType(expression.ReturnType) is not null)
        {
            return true;
        }

        var current = StripConvert(expression.Body);

        // (a ?? b) is null only when the fallback b is null.
        if (current is BinaryExpression { NodeType: ExpressionType.Coalesce, Right: var right })
        {
            current = StripConvert(right);
        }

        // The key value is nullable if the leaf or any intermediate navigation along the
        // member-access path is nullable.
        while (current is MemberExpression { Member: PropertyInfo or FieldInfo } member)
        {
            if (IsMemberNullable(member))
            {
                return true;
            }

            if (member.Expression is null)
            {
                break;
            }

            current = StripConvert(member.Expression);
        }

        // A computed key expression is treated as non-nullable.
        return false;
    }

    private static NullabilityState GetNullabilityInfoState(PropertyInfo propertyInfo)
    {
        lock (s_nullabilityInfoContextLock)
        {
            return s_nullabilityInfoContext.Create(propertyInfo).ReadState;
        }
    }

    private static NullabilityState GetNullabilityInfoState(FieldInfo fieldInfo)
    {
        lock (s_nullabilityInfoContextLock)
        {
            return s_nullabilityInfoContext.Create(fieldInfo).ReadState;
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
    /// <param name="orderExpressions">
    /// The order expressions that are used to sort the dataset.
    /// </param>
    /// <param name="orderMethods">
    /// The order methods that are used to sort the dataset.
    /// </param>
    /// <param name="forward">
    /// Defines how the dataset is sorted.
    /// </param>
    /// <param name="selector">
    /// Optional selector to apply to each item before materialization.
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
    /// If the number of keys is less than one, the number of order expressions does not match
    /// the number of order methods, if an end cursor is used without <c>before</c> and
    /// <c>last</c>, if an end cursor in <c>before</c> is combined with <c>after</c>, or if the
    /// end cursor's skip or a relative cursor's offset does not fit into an <see cref="int"/>.
    /// </exception>
    public static BatchExpression<TK, TV> BuildBatchExpression<TK, TV>(
        PagingArguments arguments,
        ReadOnlySpan<CursorKey> keys,
        ReadOnlySpan<LambdaExpression> orderExpressions,
        ReadOnlySpan<string> orderMethods,
        bool forward,
        Expression<Func<TV, TV>>? selector,
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
        var applySelectorAfterPaging = arguments.After is not null || arguments.Before is not null;

        for (var i = 0; i < orderExpressions.Length; i++)
        {
            var methodName = forward ? orderMethods[i] : ReverseOrder(orderMethods[i]);
            var orderExpression = orderExpressions[i];
            var delegateType = typeof(Func<,>).MakeGenericType(typeof(TV), orderExpression.Body.Type);
            var typedOrderExpression =
                Expression.Lambda(delegateType, orderExpression.Body, orderExpression.Parameters);

            var method = GetEnumerableMethod(methodName, typeof(TV), typedOrderExpression);

            source = Expression.Call(
                method,
                source,
                typedOrderExpression);
        }

        // The selector stays in place unless cursor filtering is active.
        if (!applySelectorAfterPaging && selector is not null)
        {
            var selectMethod = typeof(Enumerable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TV), typeof(TV));

            source = Expression.Call(selectMethod, source, selector);
        }

        var offset = 0;
        var usesRelativeCursors = false;
        Cursor? cursor = null;

        if (arguments.After is not null)
        {
            cursor = CursorParser.Parse(arguments.After, keys);

            if (cursor.IsEndCursor)
            {
                throw ThrowHelper.PagingArguments_EndCursorRequiresBeforeAndLast();
            }

            var (whereExpr, cursorOffset) = BuildWhereExpression<TV>(
                keys,
                cursor,
                forward: true,
                arguments.NullOrdering);
            source = Expression.Call(typeof(Enumerable), "Where", [typeof(TV)], source, whereExpr);
            offset = cursorOffset;

            if (cursor.IsRelative)
            {
                usesRelativeCursors = true;
            }
        }

        var isEndCursor = false;

        if (arguments.Before is not null)
        {
            if (usesRelativeCursors)
            {
                throw new ArgumentException(
                    "You cannot use `before` and `after` with relative cursors at the same time.",
                    nameof(arguments));
            }

            cursor = CursorParser.Parse(arguments.Before, keys);

            if (cursor.IsEndCursor)
            {
                if (arguments.First is not null || arguments.Last is null)
                {
                    throw ThrowHelper.PagingArguments_EndCursorRequiresBeforeAndLast();
                }

                if (arguments.After is not null)
                {
                    throw ThrowHelper.PagingArguments_EndCursorBeforeCombinedWithAfter();
                }

                isEndCursor = true;
            }
            else
            {
                var (whereExpr, cursorOffset) = BuildWhereExpression<TV>(
                    keys,
                    cursor,
                    forward: false,
                    arguments.NullOrdering);
                source = Expression.Call(typeof(Enumerable), "Where", [typeof(TV)], source, whereExpr);
                offset = cursorOffset;
            }
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

        if (isEndCursor)
        {
            var pagesBeforeLast = -cursor!.Offset!.Value;

            if (pagesBeforeLast == 0)
            {
                source = Expression.Call(
                    typeof(Enumerable),
                    "Take",
                    [typeof(TV)],
                    source,
                    Expression.Constant(requestedCount));
            }
            else
            {
                // Fetches twice the page size starting (pagesBeforeLast - 1) pages before the end
                // of each group; the exact page always fits inside that window.
                var skip = PagingQueryComposer.GetBatchEndCursorSkip(pagesBeforeLast, requestedCount);

                if (skip > 0)
                {
                    source = Expression.Call(
                        typeof(Enumerable),
                        "Skip",
                        [typeof(TV)],
                        source,
                        Expression.Constant(skip));
                }

                source = Expression.Call(
                    typeof(Enumerable),
                    "Take",
                    [typeof(TV)],
                    source,
                    Expression.Constant(requestedCount * 2));
            }
        }
        else
        {
            var absOffset = Math.Abs((long)offset);

            if (absOffset > 0)
            {
                source = Expression.Call(
                    typeof(Enumerable),
                    "Skip",
                    [typeof(TV)],
                    source,
                    Expression.Constant(PagingQueryComposer.CheckedOffsetSkip(absOffset, requestedCount)));
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
        }

        // With cursor filtering active the selector is applied after paging.
        if (applySelectorAfterPaging && selector is not null)
        {
            var selectMethod = typeof(Enumerable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TV), typeof(TV));

            source = Expression.Call(selectMethod, source, selector);
        }

        source = Expression.Call(
            typeof(Enumerable),
            "ToList",
            [typeof(TV)],
            source);

        var groupType = typeof(Group<TK, TV>);
        var bindings = new MemberBinding[]
        {
            Expression.Bind(groupType.GetProperty(nameof(Group<,>.Key))!, groupKey),
            Expression.Bind(groupType.GetProperty(nameof(Group<,>.Items))!, source)
        };

        var createGroup = Expression.MemberInit(Expression.New(groupType), bindings);
        return new BatchExpression<TK, TV>(
            Expression.Lambda<Func<IGrouping<TK, TV>, Group<TK, TV>>>(createGroup, group),
            arguments.Last is not null,
            cursor);

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
            => typeof(Enumerable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(elementType, keySelector.Body.Type);
    }

    /// <summary>
    /// Extracts and removes the orderBy and thenBy expressions from the given expression tree.
    /// </summary>
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

    /// <summary>
    /// Determines whether a member access can yield <c>null</c>: a value-type member
    /// only when it is <see cref="Nullable{T}"/>, and a reference-type member according
    /// to its nullable reference type annotation.
    /// </summary>
    private static bool IsMemberNullable(MemberExpression member)
    {
        if (member.Type.IsValueType)
        {
            return Nullable.GetUnderlyingType(member.Type) is not null;
        }

        // Unknown nullability is treated as non-nullable.
        return member.Member switch
        {
            PropertyInfo p => GetNullabilityInfoState(p) == NullabilityState.Nullable,
            FieldInfo f => GetNullabilityInfoState(f) == NullabilityState.Nullable,
            _ => false
        };
    }

    /// <summary>
    /// Removes any <see cref="ExpressionType.Convert"/> or
    /// <see cref="ExpressionType.ConvertChecked"/> wrappers from an expression and
    /// returns the underlying operand.
    /// </summary>
    private static Expression StripConvert(Expression expression)
        => expression is UnaryExpression
        {
            NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
        } unary
            ? StripConvert(unary.Operand)
            : expression;

    /// <summary>
    /// Lifts a non-nullable value-type key to its <see cref="Nullable{T}"/> form when
    /// <paramref name="keyIsNullable"/> is <c>true</c>. Keys that are already nullable or are
    /// reference types are returned unchanged.
    /// </summary>
    private static Expression LiftValueTypeIfNullable(Expression keyExpr, bool keyIsNullable)
        => keyIsNullable && keyExpr.Type.IsValueType && Nullable.GetUnderlyingType(keyExpr.Type) is null
            ? Expression.Convert(keyExpr, typeof(Nullable<>).MakeGenericType(keyExpr.Type))
            : keyExpr;

    private static Expression BuildEqualComparison(
        CursorKey cursorKey,
        Expression keyExpr,
        Expression cursorExpr)
    {
        var comparisonType = Nullable.GetUnderlyingType(keyExpr.Type) ?? keyExpr.Type;

        if (comparisonType.IsEnum)
        {
            return Expression.Equal(keyExpr, cursorExpr);
        }

        return Expression.Equal(
            Expression.Call(keyExpr, cursorKey.CompareMethod, cursorExpr),
            s_zero);
    }

    private static Expression BuildGreaterThanComparison(
        CursorKey cursorKey,
        Expression keyExpr,
        Expression cursorExpr)
    {
        var comparisonType = Nullable.GetUnderlyingType(keyExpr.Type) ?? keyExpr.Type;

        if (comparisonType.IsEnum)
        {
            var underlyingType = Enum.GetUnderlyingType(comparisonType);
            return Expression.GreaterThan(
                Expression.Convert(keyExpr, underlyingType),
                Expression.Convert(cursorExpr, underlyingType));
        }

        return Expression.GreaterThan(
            Expression.Call(keyExpr, cursorKey.CompareMethod, cursorExpr),
            s_zero);
    }

    private static Expression BuildLessThanComparison(
        CursorKey cursorKey,
        Expression keyExpr,
        Expression cursorExpr)
    {
        var comparisonType = Nullable.GetUnderlyingType(keyExpr.Type) ?? keyExpr.Type;

        if (comparisonType.IsEnum)
        {
            var underlyingType = Enum.GetUnderlyingType(comparisonType);
            return Expression.LessThan(
                Expression.Convert(keyExpr, underlyingType),
                Expression.Convert(cursorExpr, underlyingType));
        }

        return Expression.LessThan(
            Expression.Call(keyExpr, cursorKey.CompareMethod, cursorExpr),
            s_zero);
    }

    private static Expression ReplaceParameter(
        LambdaExpression expression,
        ParameterExpression replacement)
    {
        var visitor = new ReplaceParameterVisitor(expression.Parameters[0], replacement);
        return visitor.Visit(expression.Body);
    }

    /// <summary>
    /// Builds the flat, key-ordered query behind <c>ToBatchStreamPageAsync</c>: a correlated
    /// per-key ordered slice over <paramref name="source"/>, flattened by an explicit outer
    /// ordering into one row stream that a <see cref="StreamBatchPump{TKey,TElement}"/> can
    /// demultiplex.
    /// </summary>
    /// <param name="source">
    /// The queryable to be paged, already carrying the caller's key predicate and its
    /// declared ordering.
    /// </param>
    /// <param name="keySelector">
    /// Selects the key that routes a row to its page.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <param name="keys">
    /// The key definitions extracted from <paramref name="source"/>'s ordering.
    /// </param>
    /// <param name="selector">
    /// The caller's original projection, or null if the source carries no projection.
    /// </param>
    /// <param name="requestedCount">
    /// The requested page size on input; overwritten with the number derived from
    /// <paramref name="arguments"/>.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of the key that routes a row to its page.
    /// </typeparam>
    /// <typeparam name="TElement">
    /// The type of the source rows.
    /// </typeparam>
    /// <exception cref="ArgumentException">
    /// If the number of keys is zero, if an end cursor is used without <c>before</c> and
    /// <c>last</c>, if an end cursor in <c>before</c> is combined with <c>after</c>, if
    /// <c>before</c> is combined with a relative <c>after</c> cursor, if a relative cursor's
    /// offset direction does not match <c>first</c> or <c>last</c>, or if the end cursor offset
    /// or a relative cursor's offset does not fit the requested page size.
    /// </exception>
    public static BatchStreamQuery<TKey, TElement> BuildBatchStreamQuery<TKey, TElement>(
        IQueryable<TElement> source,
        Expression<Func<TElement, TKey>> keySelector,
        PagingArguments arguments,
        ReadOnlySpan<CursorKey> keys,
        Expression<Func<TElement, TElement>>? selector,
        ref int requestedCount)
        where TKey : notnull
    {
        if (keys.Length == 0)
        {
            throw ThrowHelper.Paging_NoOrderByKeys();
        }

        var forward = arguments.Last is null;
        var offset = 0;
        Cursor? cursor = null;
        Expression<Func<TElement, bool>>? afterPredicate = null;
        Expression<Func<TElement, bool>>? beforePredicate = null;
        var usesRelativeCursorsFromAfter = false;

        if (arguments.After is not null)
        {
            cursor = CursorParser.Parse(arguments.After, keys);

            if (cursor.IsEndCursor)
            {
                throw ThrowHelper.PagingArguments_EndCursorRequiresBeforeAndLast();
            }

            var (whereExpr, cursorOffset) = BuildWhereExpression<TElement>(
                keys,
                cursor,
                forward: true,
                arguments.NullOrdering);
            afterPredicate = whereExpr;
            offset = cursorOffset;
            usesRelativeCursorsFromAfter = cursor.IsRelative;
        }

        if (arguments.Before is not null)
        {
            if (usesRelativeCursorsFromAfter)
            {
                throw ThrowHelper.PagingArguments_BeforeCombinedWithRelativeAfter();
            }

            cursor = CursorParser.Parse(arguments.Before, keys);

            if (cursor.IsEndCursor)
            {
                if (arguments.First is not null || arguments.Last is null)
                {
                    throw ThrowHelper.PagingArguments_EndCursorRequiresBeforeAndLast();
                }

                if (arguments.After is not null)
                {
                    throw ThrowHelper.PagingArguments_EndCursorBeforeCombinedWithAfter();
                }
            }
            else
            {
                var (whereExpr, cursorOffset) = BuildWhereExpression<TElement>(
                    keys,
                    cursor,
                    forward: false,
                    arguments.NullOrdering);
                beforePredicate = whereExpr;
                offset = cursorOffset;
            }
        }

        if (cursor?.IsRelative == true)
        {
            if ((arguments.Last is not null && cursor.Offset > 0) || (arguments.First is not null && cursor.Offset < 0))
            {
                throw ThrowHelper.PagingArguments_RelativeOffsetDirectionMismatch();
            }
        }

        if (arguments.First is not null)
        {
            requestedCount = arguments.First.Value;
        }

        if (arguments.Last is not null)
        {
            requestedCount = arguments.Last.Value;
        }

        var isBackward = !forward;
        var isEndCursor = cursor?.IsEndCursor == true;
        var pagesBeforeLast = 0;

        int skipAmount;

        if (isEndCursor)
        {
            pagesBeforeLast = -cursor!.Offset!.Value;
            skipAmount = PagingQueryComposer.GetBatchEndCursorSkip(pagesBeforeLast, requestedCount);
        }
        else
        {
            skipAmount = PagingQueryComposer.CheckedOffsetSkip(Math.Abs((long)offset), requestedCount);
        }

        // The distinct set of requested keys, derived without the source's own ordering.
        var orderStripped = ExtractAndRemoveOrder(source.Expression).Expression;
        var keysQuery = source.Provider.CreateQuery<TElement>(orderStripped).Select(keySelector).Distinct();

        // The correlated per-key subquery: the key filter is repeated inside it, against the
        // parameter `k` of the outer SelectMany.
        var kParam = Expression.Parameter(typeof(TKey), "k");
        var xParam = Expression.Parameter(typeof(TElement), "x");
        var keyBody = ReplaceParameter(keySelector, xParam);
        var keyEqualsK = Expression.Lambda<Func<TElement, bool>>(Expression.Equal(keyBody, kParam), xParam);

        Expression body = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Where),
            [typeof(TElement)],
            source.Expression,
            Expression.Quote(keyEqualsK));

        if (afterPredicate is not null)
        {
            body = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Where),
                [typeof(TElement)],
                body,
                Expression.Quote(afterPredicate));
        }

        if (beforePredicate is not null)
        {
            body = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Where),
                [typeof(TElement)],
                body,
                Expression.Quote(beforePredicate));
        }

        if (isBackward)
        {
            // Inverts the ordering already embedded in `source`.
            var backwardQuery = ReverseOrderExpressionRewriter.Rewrite(source.Provider.CreateQuery<TElement>(body));
            body = backwardQuery.Expression;
        }

        if (skipAmount > 0)
        {
            body = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Skip),
                [typeof(TElement)],
                body,
                Expression.Constant(skipAmount));
        }

        // Forward pages take one extra row, the sentinel that answers HasNextPage.
        var takeAmount = isBackward
            ? isEndCursor && pagesBeforeLast >= 1 ? requestedCount * 2 : requestedCount
            : requestedCount + 1;
        body = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Take),
            [typeof(TElement)],
            body,
            Expression.Constant(takeAmount));

        // The caller's projection is applied once per key window.
        if (selector is not null)
        {
            body = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Select),
                [typeof(TElement), typeof(TElement)],
                body,
                Expression.Quote(selector));
        }

        var rowType = typeof(StreamBatchRow<TKey, TElement>);
        var tParam = Expression.Parameter(typeof(TElement), "t");
        var newRow = Expression.MemberInit(
            Expression.New(rowType),
            Expression.Bind(rowType.GetProperty(nameof(StreamBatchRow<TKey, TElement>.Key))!, kParam),
            Expression.Bind(rowType.GetProperty(nameof(StreamBatchRow<TKey, TElement>.Item))!, tParam));
        body = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Select),
            [typeof(TElement), rowType],
            body,
            Expression.Quote(Expression.Lambda(newRow, tParam)));

        var selectManyLambda = Expression.Lambda<Func<TKey, IEnumerable<StreamBatchRow<TKey, TElement>>>>(body, kParam);
        var flat = keysQuery.SelectMany(selectManyLambda);

        // Rows are ordered by key, then by each original key in its own direction.
        var rowParam = Expression.Parameter(rowType, "row");
        var itemAccess = Expression.Property(rowParam, nameof(StreamBatchRow<TKey, TElement>.Item));
        var outerKeySelector = Expression.Lambda<Func<StreamBatchRow<TKey, TElement>, TKey>>(
            Expression.Property(rowParam, nameof(StreamBatchRow<TKey, TElement>.Key)),
            rowParam);

        var ordered = (IQueryable<StreamBatchRow<TKey, TElement>>)flat.OrderBy(outerKeySelector);

        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var rebound = new ReplaceParameterVisitor(key.Expression.Parameters[0], itemAccess)
                .Visit(key.Expression.Body);
            var thenByLambda = Expression.Lambda(rebound, rowParam);
            var methodName = key.Direction == CursorKeyDirection.Ascending
                ? nameof(Queryable.ThenBy)
                : nameof(Queryable.ThenByDescending);
            var method = typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(rowType, rebound.Type);
            ordered = (IQueryable<StreamBatchRow<TKey, TElement>>)method.Invoke(null, [ordered, thenByLambda])!;
        }

        return new BatchStreamQuery<TKey, TElement>(
            ordered,
            isBackward,
            cursor,
            afterPredicate,
            beforePredicate,
            skipAmount);
    }

    /// <summary>
    /// Builds a grouped query that counts, per key, the number of rows in
    /// <paramref name="source"/> and, when <paramref name="predicate"/> is given, the number of
    /// rows matching it, in a single statement.
    /// </summary>
    /// <param name="source">
    /// The unsliced source to count, grouped by <paramref name="keySelector"/>.
    /// </param>
    /// <param name="keySelector">
    /// Selects the key to group by.
    /// </param>
    /// <param name="predicate">
    /// An additional predicate to count matching rows for, per key, or null to count only the
    /// full per-key total.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of the key to group by.
    /// </typeparam>
    /// <typeparam name="TElement">
    /// The type of the source rows.
    /// </typeparam>
    public static IQueryable<BatchStreamCount<TKey>> BuildBatchStreamCountsExpression<TKey, TElement>(
        IQueryable<TElement> source,
        Expression<Func<TElement, TKey>> keySelector,
        Expression<Func<TElement, bool>>? predicate)
        where TKey : notnull
    {
        var groupBy = source.GroupBy(keySelector);
        var groupingType = typeof(IGrouping<TKey, TElement>);
        var gParam = Expression.Parameter(groupingType, "g");
        var keyProperty = Expression.Property(gParam, nameof(IGrouping<TKey, TElement>.Key));

        var countMethod = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .First(m => m.Name == nameof(Enumerable.Count) && m.GetParameters().Length == 1)
            .MakeGenericMethod(typeof(TElement));
        var countCall = Expression.Call(countMethod, gParam);

        var resultType = typeof(BatchStreamCount<TKey>);
        var bindings = new List<MemberBinding>
        {
            Expression.Bind(resultType.GetProperty(nameof(BatchStreamCount<TKey>.Key))!, keyProperty),
            Expression.Bind(resultType.GetProperty(nameof(BatchStreamCount<TKey>.Count))!, countCall)
        };

        if (predicate is not null)
        {
            var countWithPredicateMethod = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == nameof(Enumerable.Count) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TElement));

            var predicateCountCall = Expression.Call(countWithPredicateMethod, gParam, predicate);
            bindings.Add(
                Expression.Bind(
                    resultType.GetProperty(nameof(BatchStreamCount<TKey>.PredicateCount))!,
                    predicateCountCall));
        }

        var selector = Expression.Lambda(Expression.MemberInit(Expression.New(resultType), bindings), gParam);
        var selectCall = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Select),
            [groupingType, resultType],
            groupBy.Expression,
            Expression.Quote(selector));

        return source.Provider.CreateQuery<BatchStreamCount<TKey>>(selectCall);
    }

    /// <summary>
    /// Combines two optional predicates into their conjunction, unified onto a single parameter.
    /// </summary>
    /// <param name="left">
    /// The first predicate, or null.
    /// </param>
    /// <param name="right">
    /// The second predicate, or null.
    /// </param>
    /// <typeparam name="TElement">
    /// The type of the element the predicates test.
    /// </typeparam>
    /// <returns>
    /// Returns whichever predicate is given when the other is null, their conjunction when both
    /// are given, or null when both are null.
    /// </returns>
    public static Expression<Func<TElement, bool>>? CombinePredicates<TElement>(
        Expression<Func<TElement, bool>>? left,
        Expression<Func<TElement, bool>>? right)
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        var parameter = left.Parameters[0];
        var rightBody = ReplaceParameter(right, parameter);
        return Expression.Lambda<Func<TElement, bool>>(Expression.AndAlso(left.Body, rightBody), parameter);
    }
}
