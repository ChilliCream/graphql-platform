using System.Linq.Expressions;
using System.Reflection;
using GreenDonut.Data.Cursors.Serializers;

namespace GreenDonut.Data.Cursors;

/// <summary>
/// Represents a cursor key of an entity type.
/// </summary>
/// <param name="expression">
/// The expression that selects the key value from the entity.
/// </param>
/// <param name="serializer">
/// The serializer that is used to serialize and deserialize the key value.
/// </param>
/// <param name="direction">
/// A value defining the sort direction of this key in dataset.
/// </param>
public sealed class CursorKey(
    LambdaExpression expression,
    ICursorKeySerializer serializer,
    CursorKeyDirection direction = CursorKeyDirection.Ascending)
{
    private Delegate? _compiled;

    /// <summary>
    /// Gets the expression that selects the key value from the entity.
    /// </summary>
    public LambdaExpression Expression { get; } = expression;

    /// <summary>
    /// Gets the compare method that is applicable to the key value.
    /// </summary>
    public MethodInfo CompareMethod { get; } = serializer.GetCompareToMethod(expression.ReturnType);

    /// <summary>
    /// Gets a value defining the sort direction of this key in dataset.
    /// </summary>
    public CursorKeyDirection Direction { get; set; } = direction;

    /// <summary>
    /// Parses the key value from a cursor.
    /// </summary>
    /// <param name="cursorValue">
    /// The span within the overall cursor that represents the key value.
    /// </param>
    /// <returns>The parsed key value.</returns>
    public object? Parse(ReadOnlySpan<byte> cursorValue)
        => CursorKeySerializerHelper.Parse(cursorValue, serializer);

    /// <summary>
    /// Tries to format the key value into a cursor.
    /// </summary>
    /// <param name="entity">
    /// The entity from which the key value should be extracted.
    /// </param>
    /// <param name="buffer">
    /// The buffer into which the key value should be written.
    /// </param>
    /// <param name="written">
    /// The number of bytes that have been written into the buffer.
    /// </param>
    /// <returns>
    /// <c>true</c> if the key value could be formatted; otherwise, <c>false</c>.
    /// </returns>
    public bool TryFormat(object entity, Span<byte> buffer, out int written)
        => CursorKeySerializerHelper.TryFormat(GetValue(entity), serializer, buffer, out written);

    private object? GetValue(object entity)
    {
        _compiled ??= NullSafeKeySelector.Compile(Expression);
        return _compiled.DynamicInvoke(entity);
    }
}

/// <summary>
/// Compiles a key-selector expression into a delegate that tolerates null
/// intermediates along a nested member-access path (for example x.Meter.Name
/// when x.Meter is null). Each nullable intermediate is bound to a local and
/// null-checked once, so the result short-circuits to null instead of throwing.
/// This mirrors the C# null-conditional operator, including its single-evaluation
/// guarantee. The serializer layer formats a null key value as the null marker.
/// </summary>
file static class NullSafeKeySelector
{
    public static Delegate Compile(LambdaExpression expression)
    {
        // Cursor-key selectors take a single entity parameter; fall back for any
        // other shape rather than assuming the parameter count.
        if (expression.Parameters.Count != 1)
        {
            return expression.Compile();
        }

        var parameter = expression.Parameters[0];

        // Drop the outer boxing-to-object conversion (if any); it is re-applied
        // at the end so every branch yields object. All other conversions are
        // kept so that a cast intermediate (for example (Derived)x.Member) keeps
        // rebinding member access against the correct type.
        var leaf = StripBoxing(expression.Body);

        // Walk the access spine, made up of member accesses and conversions,
        // from the leaf down to its root, stopping at the parameter.
        var spine = new List<Expression>();
        var current = leaf;
        while (true)
        {
            if (current is MemberExpression { Expression: { } inner })
            {
                spine.Add(current);
                current = inner;
            }
            else if (
                current is UnaryExpression
                {
                    NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked,
                    Operand: { } operand
                })
            {
                spine.Add(current);
                current = operand;
            }
            else
            {
                break;
            }
        }

        // Only rewrite a spine that roots in the parameter and has a nullable
        // intermediate (any node that gets dereferenced, i.e. all but the leaf).
        if (current != parameter || !HasNullableIntermediate(spine))
        {
            return expression.Compile();
        }

        spine.Reverse();

        var variables = new List<ParameterExpression>();
        var statements = new List<Expression>();
        var done = Expression.Label(typeof(object), "result");
        var nullResult = Expression.Constant(null, typeof(object));

        Expression expr = parameter;
        for (var i = 0; i < spine.Count; i++)
        {
            var isLeaf = i == spine.Count - 1;

            // Rebuild the node onto the running expression, preserving the
            // original member, cast, checked-ness, and conversion operator.
            expr = spine[i] is MemberExpression member
                ? member.Update(expr)
                : ((UnaryExpression)spine[i]).Update(expr);

            if (isLeaf)
            {
                statements.Add(Expression.Return(done, Expression.Convert(expr, typeof(object))));
                break;
            }

            // Bind each intermediate member to a local so it is evaluated once,
            // and guard the nullable ones so a null short-circuits instead of
            // being dereferenced. Conversions are pure, so they are applied
            // inline without a local.
            if (spine[i] is MemberExpression)
            {
                var local = Expression.Variable(expr.Type, "v" + i);
                variables.Add(local);
                statements.Add(Expression.Assign(local, expr));

                if (CanBeNull(expr.Type))
                {
                    statements.Add(
                        Expression.IfThen(
                            IsNull(local),
                            Expression.Return(done, nullResult)));
                }

                expr = local;
            }
        }

        statements.Add(Expression.Label(done, nullResult));

        var block = Expression.Block(typeof(object), variables, statements);
        return Expression.Lambda(block, parameter).Compile();
    }

    // The intermediates are every node in the spine except the leaf (index 0,
    // since the spine is collected leaf-first).
    private static bool HasNullableIntermediate(List<Expression> spine)
    {
        for (var i = 1; i < spine.Count; i++)
        {
            if (CanBeNull(spine[i].Type))
            {
                return true;
            }
        }

        return false;
    }

    // Tests an intermediate for null the same way the C# null-conditional
    // operator does: a reference-identity check for reference types (never an
    // overloaded operator ==) and a HasValue check for Nullable<T>.
    private static Expression IsNull(Expression operand)
        => Nullable.GetUnderlyingType(operand.Type) is not null
            ? Expression.Not(Expression.Property(operand, "HasValue"))
            : Expression.ReferenceEqual(operand, Expression.Constant(null, operand.Type));

    // Strips only the outer boxing-to-object conversion; other conversions carry
    // meaning (casts, widening, user-defined operators) and must be preserved.
    private static Expression StripBoxing(Expression expression)
        => expression is UnaryExpression
        {
            NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
        } unary && unary.Type == typeof(object)
            ? StripBoxing(unary.Operand)
            : expression;

    private static bool CanBeNull(Type type)
        => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}
