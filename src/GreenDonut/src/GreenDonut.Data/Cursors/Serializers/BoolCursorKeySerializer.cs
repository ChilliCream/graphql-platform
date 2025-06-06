using System.Buffers.Text;
using System.Reflection;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class BoolCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo s_compareTo = CompareToResolver.GetCompareToMethod<bool>();

    public bool IsSupported(Type type)
        => type == typeof(bool);

    public MethodInfo GetCompareToMethod(Type type)
        => s_compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        if (!Utf8Parser.TryParse(formattedKey, out byte value, out _))
        {
            throw new FormatException("The cursor value is not a valid boolean.");
        }

        return value == 1;
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
        => Utf8Formatter.TryFormat((bool)key ? (byte)1 : (byte)0, buffer, out written);
}
