using System.Buffers.Text;
using System.Reflection;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class UIntCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo s_compareTo = CompareToResolver.GetCompareToMethod<uint>();

    public bool IsSupported(Type type)
        => type == typeof(uint) || type == typeof(uint?);

    public MethodInfo GetCompareToMethod(Type type)
        => s_compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        if (!Utf8Parser.TryParse(formattedKey, out uint value, out _))
        {
            throw new FormatException("The cursor value is not a valid uint.");
        }

        return value;
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
        => Utf8Formatter.TryFormat((uint)key, buffer, out written);
}
