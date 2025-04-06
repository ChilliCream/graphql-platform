using System.Buffers.Text;
using System.Reflection;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class IntCursorKeySerializer : ICursorKeySerializer
{
    private static readonly CursorKeyCompareMethod _compareTo = CompareToResolver.GetCompareToMethod<int>();

    public bool IsSupported(Type type)
        => type == typeof(int) || type == typeof(int?);

    public bool IsNullable(Type type)
        => type == typeof(int?);

    public CursorKeyCompareMethod GetCompareToMethod(Type type)
        => _compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        if (!Utf8Parser.TryParse(formattedKey, out int value, out _))
        {
            throw new FormatException("The cursor value is not a valid integer.");
        }

        return value;
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
        => Utf8Formatter.TryFormat((int)key, buffer, out written);
}
