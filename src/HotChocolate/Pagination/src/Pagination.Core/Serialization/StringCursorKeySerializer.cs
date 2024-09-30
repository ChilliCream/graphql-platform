using System.Reflection;
using System.Text;

namespace HotChocolate.Pagination.Serialization;

internal sealed class StringCursorKeySerializer : ICursorKeySerializer
{
    private static readonly Encoding _encoding = Encoding.UTF8;
    private static readonly MethodInfo _compareTo = CompareToResolver.GetCompareToMethod<string>();

    public bool IsSupported(Type type)
        => type == typeof(string);

    public MethodInfo GetCompareToMethod(Type type)
        => _compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
        => _encoding.GetString(formattedKey);

    public bool TryFormat(object key, Span<byte> buffer, out int written)
    {
        return _encoding.TryGetBytes((string)key, buffer, out written);
    }
}
