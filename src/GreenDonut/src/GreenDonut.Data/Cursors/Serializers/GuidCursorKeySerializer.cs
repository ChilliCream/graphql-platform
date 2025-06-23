using System.Buffers.Text;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class GuidCursorKeySerializer : ICursorKeySerializer
{
    private static readonly CursorKeyCompareMethod s_compareTo = CompareToResolver.GetCompareToMethod<Guid>();

    public bool IsSupported(Type type)
        => type == typeof(Guid) || type == typeof(Guid?);

    public bool IsNullable(Type type)
        => type == typeof(Guid?);

    public CursorKeyCompareMethod GetCompareToMethod(Type type)
        => s_compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        if (!Utf8Parser.TryParse(formattedKey, out Guid value, out _))
        {
            throw new FormatException("The cursor value is not a valid guid.");
        }

        return value;
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
        => Utf8Formatter.TryFormat((Guid)key, buffer, out written);
}
