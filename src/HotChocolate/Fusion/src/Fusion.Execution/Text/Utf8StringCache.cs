using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Caching.Memory;

namespace HotChocolate.Fusion.Text;

internal static class Utf8StringCache
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;
    private static readonly Cache<byte[]> s_propertyNameCache = new(capacity: 4 * 1024);

    /// <summary>
    /// Gets the UTF-8 representation of <paramref name="s"/> as a JSON property name token,
    /// wrapped in quotes and followed by a colon. The name is not escaped.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetJsonPropertyName(string s)
        => s_propertyNameCache.GetOrCreate(s, static k => CreatePropertyName(k));

    private static byte[] CreatePropertyName(string s)
    {
        var bytes = new byte[s_utf8.GetByteCount(s) + 3];
        bytes[0] = (byte)'"';
        s_utf8.GetBytes(s, bytes.AsSpan(1));
        bytes[^2] = (byte)'"';
        bytes[^1] = (byte)':';
        return bytes;
    }
}
