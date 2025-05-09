using System.Buffers.Text;
using System.Reflection;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class UShortCursorKeySerializer : ICursorKeySerializer
{
    private static readonly CursorKeyCompareMethod _compareTo = CompareToResolver.GetCompareToMethod<ushort>();

    public bool IsSupported(Type type)
        => type == typeof(ushort) || type == typeof(ushort?);

    public bool IsNullable(Type type)
        => type == typeof(ushort?);

    public CursorKeyCompareMethod GetCompareToMethod(Type type)
        => _compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        if (!Utf8Parser.TryParse(formattedKey, out ushort value, out _))
        {
            throw new FormatException("The cursor value is not a valid ushort.");
        }

        return value;
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
        => Utf8Formatter.TryFormat((ushort)key, buffer, out written);
}
