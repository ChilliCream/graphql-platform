using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Pagination.Serialization;

namespace HotChocolate.Pagination.Expressions;

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
    /// <returns></returns>
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
        _compiled ??= Expression.Compile();
        return _compiled.DynamicInvoke(entity);
    }
}
