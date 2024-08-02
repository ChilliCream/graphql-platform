using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Pagination.Serialization;

namespace HotChocolate.Pagination;

internal sealed class CursorKey(
    LambdaExpression expression,
    ICursorKeySerializer serializer,
    bool ascending = true)
{
    public LambdaExpression Expression { get; } = expression;

    public MethodInfo CompareMethod { get; } = serializer.GetCompareToMethod(expression.ReturnType);

    public bool Ascending { get; set; } = ascending;

    public object Parse(ReadOnlySpan<byte> cursorValue)
        => serializer.Parse(cursorValue);

    public bool TryFormat(object entity, Span<byte> buffer, out int written)
    {
        var key = GetValue(entity);

        if (key is null)
        {
            written = 0;
            return true;
        }

        return serializer.TryFormat(key, buffer, out written);
    }

    private Delegate? _compiled;

    private object? GetValue(object entity)
    {
        _compiled ??= Expression.Compile();

        return _compiled.DynamicInvoke(entity);
    }
}
