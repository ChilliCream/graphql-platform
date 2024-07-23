using System.Buffers.Text;
using System.Reflection;

namespace HotChocolate.Pagination.Serialization;

internal sealed class FloatCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo _compareTo = CompareToResolver.GetCompareToMethod<float>();

    public bool IsSupported(Type type)
        => type == typeof(float);

    public MethodInfo GetCompareToMethod(Type type)
        => _compareTo;

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
