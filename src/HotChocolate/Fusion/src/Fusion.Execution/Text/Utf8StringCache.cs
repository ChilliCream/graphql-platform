using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Caching.Memory;

namespace HotChocolate.Fusion.Text;

internal static class Utf8StringCache
{
    /// <summary>
    /// The number of zero bytes that follow the token in the arrays returned by
    /// <see cref="GetJsonPropertyName"/>.
    /// </summary>
    public const int JsonPropertyNamePadding = 32;

    private static readonly Encoding s_utf8 = Encoding.UTF8;
    private static readonly Cache<byte[]> s_propertyNameCache = new(capacity: 4 * 1024);

    /// <summary>
    /// Gets the UTF-8 representation of <paramref name="s"/> as a JSON property name token,
    /// wrapped in quotes and followed by a colon and <see cref="JsonPropertyNamePadding"/>
    /// zero bytes. The name is not escaped.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetJsonPropertyName(string s)
        => s_propertyNameCache.GetOrCreate(s, static k => CreatePropertyName(k));

    private static byte[] CreatePropertyName(string s)
    {
        var tokenLength = s_utf8.GetByteCount(s) + 3;
        var bytes = new byte[tokenLength + JsonPropertyNamePadding];
        bytes[0] = (byte)'"';
        s_utf8.GetBytes(s, bytes.AsSpan(1));
        bytes[tokenLength - 2] = (byte)'"';
        bytes[tokenLength - 1] = (byte)':';
        return bytes;
    }
}
