#if !NETSTANDARD2_0
using System.Buffers.Binary;
using System.Numerics;

namespace HotChocolate.Language;

/// <summary>
/// Computes SHA-256 digests in managed code. The digest is an identifier and must not be used
/// to protect data.
/// </summary>
internal static class Sha256Digest
{
    /// <summary>
    /// The size of a SHA-256 digest in bytes.
    /// </summary>
    public const int SizeInBytes = 32;

    private const int BlockSize = 64;

    private static ReadOnlySpan<uint> RoundConstants =>
    [
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5,
        0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3,
        0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc,
        0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7,
        0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13,
        0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3,
        0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5,
        0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208,
        0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
    ];

    /// <summary>
    /// Computes the SHA-256 digest of <paramref name="source"/> and writes it to the first
    /// <see cref="SizeInBytes"/> bytes of <paramref name="destination"/>.
    /// </summary>
    public static void Compute(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        destination = destination[..SizeInBytes];

        Span<uint> state =
        [
            0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a,
            0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19
        ];
        Span<uint> schedule = stackalloc uint[64];
        var remaining = source;

        while (remaining.Length >= BlockSize)
        {
            Transform(state, schedule, remaining);
            remaining = remaining[BlockSize..];
        }

        // The last partial block is padded with the 0x80 marker and the message length in bits,
        // which takes one block, or two when fewer than nine bytes are free in the first.
        Span<byte> padded = stackalloc byte[BlockSize * 2];
        padded.Clear();
        remaining.CopyTo(padded);
        padded[remaining.Length] = 0x80;

        var paddedLength = remaining.Length < BlockSize - 8 ? BlockSize : BlockSize * 2;
        BinaryPrimitives.WriteUInt64BigEndian(padded[(paddedLength - 8)..], (ulong)source.Length * 8);

        Transform(state, schedule, padded);

        if (paddedLength > BlockSize)
        {
            Transform(state, schedule, padded[BlockSize..]);
        }

        for (var i = 0; i < state.Length; i++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination[(i * 4)..], state[i]);
        }
    }

    private static void Transform(Span<uint> state, Span<uint> schedule, ReadOnlySpan<byte> block)
    {
        var w = schedule[..64];
        var k = RoundConstants;

        for (var t = 0; t < 16; t++)
        {
            w[t] = BinaryPrimitives.ReadUInt32BigEndian(block[(t * 4)..]);
        }

        for (var t = 16; t < w.Length; t++)
        {
            var w15 = w[t - 15];
            var w2 = w[t - 2];
            var s0 = BitOperations.RotateRight(w15, 7) ^ BitOperations.RotateRight(w15, 18) ^ (w15 >> 3);
            var s1 = BitOperations.RotateRight(w2, 17) ^ BitOperations.RotateRight(w2, 19) ^ (w2 >> 10);
            w[t] = w[t - 16] + s0 + w[t - 7] + s1;
        }

        var a = state[0];
        var b = state[1];
        var c = state[2];
        var d = state[3];
        var e = state[4];
        var f = state[5];
        var g = state[6];
        var h = state[7];

        for (var t = 0; t < w.Length; t++)
        {
            var sum1 = BitOperations.RotateRight(e, 6)
                ^ BitOperations.RotateRight(e, 11)
                ^ BitOperations.RotateRight(e, 25);
            var choice = (e & f) ^ (~e & g);
            var temp1 = h + sum1 + choice + k[t] + w[t];
            var sum0 = BitOperations.RotateRight(a, 2)
                ^ BitOperations.RotateRight(a, 13)
                ^ BitOperations.RotateRight(a, 22);
            var majority = (a & b) ^ (a & c) ^ (b & c);
            var temp2 = sum0 + majority;

            h = g;
            g = f;
            f = e;
            e = d + temp1;
            d = c;
            c = b;
            b = a;
            a = temp1 + temp2;
        }

        state[0] += a;
        state[1] += b;
        state[2] += c;
        state[3] += d;
        state[4] += e;
        state[5] += f;
        state[6] += g;
        state[7] += h;
    }
}
#endif
