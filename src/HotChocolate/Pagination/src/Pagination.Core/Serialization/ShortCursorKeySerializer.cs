using System.Buffers.Text;
using System.Reflection;

namespace HotChocolate.Pagination.Serialization;

internal sealed class ShortCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo _compareTo = CompareToResolver.GetCompareToMethod<short>();

    public bool IsSupported(Type type)
        => type == typeof(short);

    public MethodInfo GetCompareToMethod(Type type)
        => _compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        if (!Utf8Parser.TryParse(formattedKey, out short value, out _))
        {
            throw new FormatException("The cursor value is not a valid integer.");
        }

        return value;
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
        => Utf8Formatter.TryFormat((short)key, buffer, out written);
}
