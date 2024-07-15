using System.Buffers.Text;
using System.Reflection;

namespace HotChocolate.Pagination.Serialization;

internal sealed class GuidCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo _compareTo = CompareToResolver.GetCompareToMethod<Guid>();

    public bool IsSupported(Type type)
        => type == typeof(Guid);

    public MethodInfo GetCompareToMethod(Type type)
        => _compareTo;

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

internal static class CompareToResolver
{
    private const string _compareTo = "CompareTo";

    public static MethodInfo GetCompareToMethod<T>()
        => GetCompareToMethod(typeof(T));

    public static MethodInfo GetCompareToMethod(Type type)
        => type.GetMethod(_compareTo, [type])!;
}
