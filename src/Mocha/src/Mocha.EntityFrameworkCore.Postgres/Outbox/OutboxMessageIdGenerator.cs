using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Mocha.Outbox;

/// <summary>
/// Generates UUIDv7 outbox message identifiers that strictly increase in uuid byte order within the process.
/// </summary>
internal static class OutboxMessageIdGenerator
{
    private const int CounterBits = 16;
    private const ulong CounterMask = (1UL << CounterBits) - 1;

    // the unix millisecond timestamp in the upper 48 bits and the counter in the lower 16 bits
    private static ulong s_state;

    /// <summary>
    /// Returns an identifier that is greater, in uuid byte order, than every identifier returned before it.
    /// </summary>
    public static Guid Next()
    {
        var state = NextState();
        var timestamp = state >> CounterBits;
        var counter = (int)(state & CounterMask);

        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, timestamp << 16);
        RandomNumberGenerator.Fill(bytes[8..]);

        // version 7 and the upper 12 counter bits fill rand_a, the variant and the lower 4 counter bits
        // start rand_b, the remaining rand_b bits stay random
        bytes[6] = (byte)(0x70 | (counter >> 12));
        bytes[7] = (byte)(counter >> 4);
        bytes[8] = (byte)(0x80 | ((counter & 0x0F) << 2) | (bytes[8] & 0x03));

        return new Guid(bytes, bigEndian: true);
    }

    private static ulong NextState()
    {
        while (true)
        {
            var last = Volatile.Read(ref s_state);
            var now = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() << CounterBits;

            // the same millisecond or a clock that moved backwards increments the counter,
            // and a counter overflow carries into the timestamp
            var next = Math.Max(now, last + 1);

            if (Interlocked.CompareExchange(ref s_state, next, last) == last)
            {
                return next;
            }
        }
    }
}
