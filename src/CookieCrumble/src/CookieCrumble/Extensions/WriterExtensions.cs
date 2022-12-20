using System.Buffers;
using System.Text;

namespace CookieCrumble;

public static class WriterExtensions
{
    private static readonly Encoding _utf8 = Encoding.UTF8;

    public static void Append(this IBufferWriter<byte> snapshot, string value)
        => Append(snapshot, value.AsSpan());

#if NET5_0_OR_GREATER
    public static void Append(this IBufferWriter<byte> snapshot, ReadOnlySpan<char> value)
#else
    public static unsafe void Append(this IBufferWriter<byte> snapshot, ReadOnlySpan<char> value)
#endif
    {
#if NET5_0_OR_GREATER
        _utf8.GetBytes(value, snapshot);
#else
        var length = _utf8.GetByteCount(value);
        byte[]? buffer = null;
        var span = length <= 256
            ? stackalloc byte[length]
            : buffer = ArrayPool<byte>.Shared.Rent(length);
        int written;

        fixed (byte* bptr = span)
        {
            fixed (char* cprt = value)
            {
                written = _utf8.GetBytes(cprt, value.Length, bptr, length);
            }
        }

        var snapshotSpan = snapshot.GetSpan(written);
        span[..written].CopyTo(snapshotSpan);
        snapshot.Advance(written);

        if (buffer is not null)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
#endif
    }

    public static void AppendLine(this IBufferWriter<byte> snapshot)
    {
        snapshot.GetSpan(1)[0] = (byte)'\n';
        snapshot.Advance(1);
    }

    public static void AppendSeparator(this IBufferWriter<byte> snapshot)
    {
        const byte hyphen = (byte)'-';
        var span = snapshot.GetSpan(15);

        for(var i = 0; i < 15; i++)
        {
            span[i] = hyphen;
        }

        snapshot.Advance(15);
    }
}
