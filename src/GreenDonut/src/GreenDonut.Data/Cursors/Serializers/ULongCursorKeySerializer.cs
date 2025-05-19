using System.Buffers.Text;
using System.Reflection;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class ULongCursorKeySerializer : ICursorKeySerializer
{
    private static readonly CursorKeyCompareMethod _compareTo = CompareToResolver.GetCompareToMethod<ulong>();

    public bool IsSupported(Type type)
        => type == typeof(ulong) || type == typeof(ulong?);

    public bool IsNullable(Type type)
        => type == typeof(ulong?);

    public CursorKeyCompareMethod GetCompareToMethod(Type type)
        => _compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        if (!Utf8Parser.TryParse(formattedKey, out ulong value, out _))
        {
            throw new FormatException("The cursor value is not a valid ulong.");
        }

        return value;
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
        => Utf8Formatter.TryFormat((ulong)key, buffer, out written);
}
