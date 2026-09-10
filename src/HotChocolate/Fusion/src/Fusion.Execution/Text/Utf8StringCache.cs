using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Caching.Memory;

namespace HotChocolate.Fusion.Text;

internal static class Utf8StringCache
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;
    private static readonly Cache<byte[]> s_cache = new(capacity: 4 * 1024);
    private static readonly Cache<byte[]> s_quotedCache = new(capacity: 4 * 1024);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetUtf8String(string s)
        => s_cache.GetOrCreate(s, static k => s_utf8.GetBytes(k));

    /// <summary>
    /// Gets the UTF-8 representation of <paramref name="s"/> wrapped in JSON string quotes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetQuotedUtf8String(string s)
        => s_quotedCache.GetOrCreate(s, static k => Quote(k));

    private static byte[] Quote(string s)
    {
        var bytes = new byte[s_utf8.GetByteCount(s) + 2];
        bytes[0] = (byte)'"';
        s_utf8.GetBytes(s, bytes.AsSpan(1));
        bytes[^1] = (byte)'"';
        return bytes;
    }
}
