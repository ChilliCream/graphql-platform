using System.Buffers.Text;
using System.Reflection;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class DoubleCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo s_compareTo = CompareToResolver.GetCompareToMethod<double>();

    public bool IsSupported(Type type)
        => type == typeof(double);

    public MethodInfo GetCompareToMethod(Type type)
        => s_compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        if (!Utf8Parser.TryParse(formattedKey, out double value, out _))
        {
            throw new FormatException("The cursor value is not a valid double.");
        }

        return value;
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
        => Utf8Formatter.TryFormat((double)key, buffer, out written);
}
