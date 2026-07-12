using System.Buffers;
using System.IO.Hashing;
using System.Text;

namespace HotChocolate.Fusion.Execution;

internal static class FusionStringExtensions
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;
    private static readonly ArrayPool<byte> s_arrayPool = ArrayPool<byte>.Shared;

    public static ulong ComputeHash(this string s)
    {
        var maxByteCount = s_utf8.GetMaxByteCount(s.Length);
        byte[]? rentedBuffer = null;
        var buffer = maxByteCount < 256
            ? stackalloc byte[256]
            : s_arrayPool.Rent(maxByteCount);

        try
        {
            var byteCount = s_utf8.GetBytes(s, buffer);
            buffer = buffer[..byteCount];
            return XxHash64.HashToUInt64(buffer);
        }
        finally
        {
            if (rentedBuffer is not null)
            {
                buffer.Clear();
                s_arrayPool.Return(rentedBuffer);
            }
        }
    }
}
