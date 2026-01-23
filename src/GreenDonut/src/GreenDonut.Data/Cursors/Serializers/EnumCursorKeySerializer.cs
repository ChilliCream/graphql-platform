using System.Buffers.Text;
using System.Numerics;
using System.Reflection;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class EnumCursorKeySerializer<T> : ICursorKeySerializer where T : struct, INumber<T>
{
    private static readonly MethodInfo _compareTo = CompareToResolver.GetCompareToMethod<T>();

    public bool IsSupported(Type type)
        => type.IsEnum && Enum.GetUnderlyingType(type) == typeof(T);

    public MethodInfo GetCompareToMethod(Type type)
        => _compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        var t = typeof(T);

        return t switch
        {
            _ when t == typeof(byte) && Utf8Parser.TryParse(formattedKey, out byte b, out _)
                => b,
            _ when t == typeof(sbyte) && Utf8Parser.TryParse(formattedKey, out sbyte sb, out _)
                => sb,
            _ when t == typeof(short) && Utf8Parser.TryParse(formattedKey, out short s, out _)
                => s,
            _ when t == typeof(ushort) && Utf8Parser.TryParse(formattedKey, out ushort us, out _)
                => us,
            _ when t == typeof(int) && Utf8Parser.TryParse(formattedKey, out int i, out _)
                => i,
            _ when t == typeof(uint) && Utf8Parser.TryParse(formattedKey, out uint ui, out _)
                => ui,
            _ when t == typeof(long) && Utf8Parser.TryParse(formattedKey, out long l, out _)
                => l,
            _ when t == typeof(ulong) && Utf8Parser.TryParse(formattedKey, out ulong ul, out _)
                => ul,
            _ => throw new InvalidOperationException("Unsupported enum type.")
        };
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
    {
        var t = typeof(T);

        return t switch
        {
            _ when t == typeof(byte) => Utf8Formatter.TryFormat((byte)key, buffer, out written),
            _ when t == typeof(sbyte) => Utf8Formatter.TryFormat((sbyte)key, buffer, out written),
            _ when t == typeof(short) => Utf8Formatter.TryFormat((short)key, buffer, out written),
            _ when t == typeof(ushort) => Utf8Formatter.TryFormat((ushort)key, buffer, out written),
            _ when t == typeof(int) => Utf8Formatter.TryFormat((int)key, buffer, out written),
            _ when t == typeof(uint) => Utf8Formatter.TryFormat((uint)key, buffer, out written),
            _ when t == typeof(long) => Utf8Formatter.TryFormat((long)key, buffer, out written),
            _ when t == typeof(ulong) => Utf8Formatter.TryFormat((ulong)key, buffer, out written),
            _ => throw new InvalidOperationException("Unsupported enum type.")
        };
    }
}
