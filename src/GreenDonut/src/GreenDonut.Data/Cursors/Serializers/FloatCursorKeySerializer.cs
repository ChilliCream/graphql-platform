using System.Buffers.Text;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class FloatCursorKeySerializer : ICursorKeySerializer
{
    private static readonly CursorKeyCompareMethod s_compareTo = CompareToResolver.GetCompareToMethod<float>();

    public bool IsSupported(Type type)
        => type == typeof(float) || type == typeof(float?);

    public bool IsNullable(Type type)
        => type == typeof(float?);

    public CursorKeyCompareMethod GetCompareToMethod(Type type)
        => s_compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        if (!Utf8Parser.TryParse(formattedKey, out float value, out _))
        {
            throw new FormatException("The cursor value is not a valid float.");
        }

        return value;
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
        => Utf8Formatter.TryFormat((float)key, buffer, out written);
}
