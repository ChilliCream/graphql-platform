using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Pagination.Serialization;

namespace HotChocolate.Pagination.Expressions;

public sealed class CursorKey(
    LambdaExpression expression,
    ICursorKeySerializer serializer,
    bool ascending = true)
{
    public LambdaExpression Expression { get; } = expression;

    public MethodInfo CompareMethod { get; } = serializer.GetCompareToMethod(expression.ReturnType);

    public bool Ascending { get; set; } = ascending;

    public object? Parse(ReadOnlySpan<byte> cursorValue)
        => CursorKeySerializerHelper.Parse(cursorValue, serializer);

    public bool TryFormat(object entity, Span<byte> buffer, out int written)
        => CursorKeySerializerHelper.TryFormat(GetValue(entity), serializer, buffer, out written);

    private Delegate? _compiled;

    private object? GetValue(object entity)
    {
        _compiled ??= Expression.Compile();

        return _compiled.DynamicInvoke(entity);
    }
}
