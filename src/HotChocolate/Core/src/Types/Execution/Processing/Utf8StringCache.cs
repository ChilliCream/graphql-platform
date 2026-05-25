using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Caching.Memory;

namespace HotChocolate.Execution.Processing;

internal static class Utf8StringCache
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;
    private static readonly Cache<byte[]> s_cache = new(capacity: 4 * 1024);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetUtf8String(string s)
        => s_cache.GetOrCreate(s, static k => s_utf8.GetBytes(k));
}
