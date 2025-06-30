using System.Text;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class StringCursorKeySerializer : ICursorKeySerializer
{
    private static readonly Encoding s_encoding = Encoding.UTF8;
    private static readonly CursorKeyCompareMethod s_compareTo = CompareToResolver.GetCompareToMethod<string>();

    public bool IsSupported(Type type)
        => type == typeof(string);

    public bool IsNullable(Type type)
        => false;

    public CursorKeyCompareMethod GetCompareToMethod(Type type)
        => s_compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
        => s_encoding.GetString(formattedKey);

    public bool TryFormat(object key, Span<byte> buffer, out int written)
    {
        return s_encoding.TryGetBytes((string)key, buffer, out written);
    }
}
