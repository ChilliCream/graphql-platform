using System.Buffers.Text;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class LongCursorKeySerializer : ICursorKeySerializer
{
    private static readonly CursorKeyCompareMethod s_compareTo = CompareToResolver.GetCompareToMethod<long>();

    public bool IsSupported(Type type)
        => type == typeof(long) || type == typeof(long?);

    public bool IsNullable(Type type)
        => type == typeof(long?);

    public CursorKeyCompareMethod GetCompareToMethod(Type type)
        => s_compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        if (!Utf8Parser.TryParse(formattedKey, out long value, out _))
        {
            throw new FormatException("The cursor value is not a valid long.");
        }

        return value;
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
        => Utf8Formatter.TryFormat((long)key, buffer, out written);
}
