using System.Reflection;
using HotChocolate.Pagination.Serialization;

namespace HotChocolate.Pagination;

internal sealed class CursorKey(
    PropertyInfo property,
    ICursorKeySerializer serializer,
    bool ascending = true)
{
    public PropertyInfo Property { get; } = property;

    public MethodInfo CompareMethod { get; } = serializer.GetCompareToMethod(property.PropertyType);

    public bool Ascending { get; set; } = ascending;

    public object Parse(ReadOnlySpan<byte> cursorValue)
        => serializer.Parse(cursorValue);

    public bool TryFormat(object entity, Span<byte> buffer, out int written)
    {
        var key = Property.GetValue(entity)!;
        return serializer.TryFormat(key, buffer, out written);
    }
}
