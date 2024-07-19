using System.Reflection;
using System.Text;

namespace HotChocolate.Pagination.Serialization;

internal sealed class StringCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo _compareTo = CompareToResolver.GetCompareToMethod<string>();

    public bool IsSupported(Type type)
        => type == typeof(string);

    public MethodInfo GetCompareToMethod(Type type)
        => _compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
        => Encoding.UTF8.GetString(formattedKey);

    public bool TryFormat(object key, Span<byte> buffer, out int written)
    {
#if NET8_0_OR_GREATER
        return Encoding.UTF8.TryGetBytes((string)key, buffer, out written);
#else
        written = Encoding.UTF8.GetBytes((string)key, buffer);
        return true;
#endif
    }
}
