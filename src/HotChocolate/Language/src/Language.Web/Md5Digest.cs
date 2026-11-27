#if !NETSTANDARD2_0
using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;

namespace HotChocolate.Language;

/// <summary>
/// Computes MD5 digests in managed code. The digest identifies a document and must not be used
/// to protect data.
/// </summary>
internal static class Md5Digest
{
    /// <summary>
    /// The size of an MD5 digest in bytes.
    /// </summary>
    public const int SizeInBytes = 16;

    private const int BlockSize = 64;

    /// <summary>
    /// Computes the MD5 digest of <paramref name="source"/> and writes it to the first
    /// <see cref="SizeInBytes"/> bytes of <paramref name="destination"/>.
    /// </summary>
    public static void Compute(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        destination = destination[..SizeInBytes];

        var state = State.Create();
        var remaining = source;

        while (remaining.Length >= BlockSize)
        {
            Transform(ref state, remaining);
            remaining = remaining[BlockSize..];
        }

        Finish(ref state, remaining, source.Length, destination);
    }

    /// <summary>
    /// Computes the MD5 digest of <paramref name="source"/> and writes it to the first
    /// <see cref="SizeInBytes"/> bytes of <paramref name="destination"/>.
    /// </summary>
    public static void Compute(ReadOnlySequence<byte> source, Span<byte> destination)
    {
        destination = destination[..SizeInBytes];

        var state = State.Create();
        Span<byte> pending = stackalloc byte[BlockSize];
        var pendingLength = 0;
        var length = 0L;

        foreach (var segment in source)
        {
            var remaining = segment.Span;
            length += remaining.Length;

            if (pendingLength > 0)
            {
                var take = Math.Min(BlockSize - pendingLength, remaining.Length);
                remaining[..take].CopyTo(pending[pendingLength..]);
                pendingLength += take;
                remaining = remaining[take..];

                if (pendingLength < BlockSize)
                {
                    continue;
                }

                Transform(ref state, pending);
                pendingLength = 0;
            }

            while (remaining.Length >= BlockSize)
            {
                Transform(ref state, remaining);
                remaining = remaining[BlockSize..];
            }

            remaining.CopyTo(pending);
            pendingLength = remaining.Length;
        }

        Finish(ref state, pending[..pendingLength], length, destination);
    }

    // Pads the last partial block with the 0x80 marker and the message length in bits, which
    // takes one block, or two when fewer than nine bytes are free in the first.
    private static void Finish(
        ref State state,
        ReadOnlySpan<byte> tail,
        long length,
        Span<byte> destination)
    {
        Span<byte> padded = stackalloc byte[BlockSize * 2];
        padded.Clear();
        tail.CopyTo(padded);
        padded[tail.Length] = 0x80;

        var paddedLength = tail.Length < BlockSize - 8 ? BlockSize : BlockSize * 2;
        BinaryPrimitives.WriteUInt64LittleEndian(padded[(paddedLength - 8)..], (ulong)length * 8);

        Transform(ref state, padded);

        if (paddedLength > BlockSize)
        {
            Transform(ref state, padded[BlockSize..]);
        }

        BinaryPrimitives.WriteUInt32LittleEndian(destination, state.A);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[4..], state.B);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[8..], state.C);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[12..], state.D);
    }

    private static void Transform(ref State state, ReadOnlySpan<byte> block)
    {
        block = block[..BlockSize];

        var m0 = BinaryPrimitives.ReadUInt32LittleEndian(block);
        var m1 = BinaryPrimitives.ReadUInt32LittleEndian(block[4..]);
        var m2 = BinaryPrimitives.ReadUInt32LittleEndian(block[8..]);
        var m3 = BinaryPrimitives.ReadUInt32LittleEndian(block[12..]);
        var m4 = BinaryPrimitives.ReadUInt32LittleEndian(block[16..]);
        var m5 = BinaryPrimitives.ReadUInt32LittleEndian(block[20..]);
        var m6 = BinaryPrimitives.ReadUInt32LittleEndian(block[24..]);
        var m7 = BinaryPrimitives.ReadUInt32LittleEndian(block[28..]);
        var m8 = BinaryPrimitives.ReadUInt32LittleEndian(block[32..]);
        var m9 = BinaryPrimitives.ReadUInt32LittleEndian(block[36..]);
        var m10 = BinaryPrimitives.ReadUInt32LittleEndian(block[40..]);
        var m11 = BinaryPrimitives.ReadUInt32LittleEndian(block[44..]);
        var m12 = BinaryPrimitives.ReadUInt32LittleEndian(block[48..]);
        var m13 = BinaryPrimitives.ReadUInt32LittleEndian(block[52..]);
        var m14 = BinaryPrimitives.ReadUInt32LittleEndian(block[56..]);
        var m15 = BinaryPrimitives.ReadUInt32LittleEndian(block[60..]);

        var a = state.A;
        var b = state.B;
        var c = state.C;
        var d = state.D;

        // Every step adds the term that reads the word of the previous step last. The two terms of
        // the second round function share no bits, so they are added.
        a = b + BitOperations.RotateLeft((a + m0 + 0xd76aa478u) + (d ^ (b & (c ^ d))), 7);
        d = a + BitOperations.RotateLeft((d + m1 + 0xe8c7b756u) + (c ^ (a & (b ^ c))), 12);
        c = d + BitOperations.RotateLeft((c + m2 + 0x242070dbu) + (b ^ (d & (a ^ b))), 17);
        b = c + BitOperations.RotateLeft((b + m3 + 0xc1bdceeeu) + (a ^ (c & (d ^ a))), 22);
        a = b + BitOperations.RotateLeft((a + m4 + 0xf57c0fafu) + (d ^ (b & (c ^ d))), 7);
        d = a + BitOperations.RotateLeft((d + m5 + 0x4787c62au) + (c ^ (a & (b ^ c))), 12);
        c = d + BitOperations.RotateLeft((c + m6 + 0xa8304613u) + (b ^ (d & (a ^ b))), 17);
        b = c + BitOperations.RotateLeft((b + m7 + 0xfd469501u) + (a ^ (c & (d ^ a))), 22);
        a = b + BitOperations.RotateLeft((a + m8 + 0x698098d8u) + (d ^ (b & (c ^ d))), 7);
        d = a + BitOperations.RotateLeft((d + m9 + 0x8b44f7afu) + (c ^ (a & (b ^ c))), 12);
        c = d + BitOperations.RotateLeft((c + m10 + 0xffff5bb1u) + (b ^ (d & (a ^ b))), 17);
        b = c + BitOperations.RotateLeft((b + m11 + 0x895cd7beu) + (a ^ (c & (d ^ a))), 22);
        a = b + BitOperations.RotateLeft((a + m12 + 0x6b901122u) + (d ^ (b & (c ^ d))), 7);
        d = a + BitOperations.RotateLeft((d + m13 + 0xfd987193u) + (c ^ (a & (b ^ c))), 12);
        c = d + BitOperations.RotateLeft((c + m14 + 0xa679438eu) + (b ^ (d & (a ^ b))), 17);
        b = c + BitOperations.RotateLeft((b + m15 + 0x49b40821u) + (a ^ (c & (d ^ a))), 22);

        a = b + BitOperations.RotateLeft((a + m1 + 0xf61e2562u + (~d & c)) + (d & b), 5);
        d = a + BitOperations.RotateLeft((d + m6 + 0xc040b340u + (~c & b)) + (c & a), 9);
        c = d + BitOperations.RotateLeft((c + m11 + 0x265e5a51u + (~b & a)) + (b & d), 14);
        b = c + BitOperations.RotateLeft((b + m0 + 0xe9b6c7aau + (~a & d)) + (a & c), 20);
        a = b + BitOperations.RotateLeft((a + m5 + 0xd62f105du + (~d & c)) + (d & b), 5);
        d = a + BitOperations.RotateLeft((d + m10 + 0x02441453u + (~c & b)) + (c & a), 9);
        c = d + BitOperations.RotateLeft((c + m15 + 0xd8a1e681u + (~b & a)) + (b & d), 14);
        b = c + BitOperations.RotateLeft((b + m4 + 0xe7d3fbc8u + (~a & d)) + (a & c), 20);
        a = b + BitOperations.RotateLeft((a + m9 + 0x21e1cde6u + (~d & c)) + (d & b), 5);
        d = a + BitOperations.RotateLeft((d + m14 + 0xc33707d6u + (~c & b)) + (c & a), 9);
        c = d + BitOperations.RotateLeft((c + m3 + 0xf4d50d87u + (~b & a)) + (b & d), 14);
        b = c + BitOperations.RotateLeft((b + m8 + 0x455a14edu + (~a & d)) + (a & c), 20);
        a = b + BitOperations.RotateLeft((a + m13 + 0xa9e3e905u + (~d & c)) + (d & b), 5);
        d = a + BitOperations.RotateLeft((d + m2 + 0xfcefa3f8u + (~c & b)) + (c & a), 9);
        c = d + BitOperations.RotateLeft((c + m7 + 0x676f02d9u + (~b & a)) + (b & d), 14);
        b = c + BitOperations.RotateLeft((b + m12 + 0x8d2a4c8au + (~a & d)) + (a & c), 20);

        a = b + BitOperations.RotateLeft((a + m5 + 0xfffa3942u) + ((c ^ d) ^ b), 4);
        d = a + BitOperations.RotateLeft((d + m8 + 0x8771f681u) + ((b ^ c) ^ a), 11);
        c = d + BitOperations.RotateLeft((c + m11 + 0x6d9d6122u) + ((a ^ b) ^ d), 16);
        b = c + BitOperations.RotateLeft((b + m14 + 0xfde5380cu) + ((d ^ a) ^ c), 23);
        a = b + BitOperations.RotateLeft((a + m1 + 0xa4beea44u) + ((c ^ d) ^ b), 4);
        d = a + BitOperations.RotateLeft((d + m4 + 0x4bdecfa9u) + ((b ^ c) ^ a), 11);
        c = d + BitOperations.RotateLeft((c + m7 + 0xf6bb4b60u) + ((a ^ b) ^ d), 16);
        b = c + BitOperations.RotateLeft((b + m10 + 0xbebfbc70u) + ((d ^ a) ^ c), 23);
        a = b + BitOperations.RotateLeft((a + m13 + 0x289b7ec6u) + ((c ^ d) ^ b), 4);
        d = a + BitOperations.RotateLeft((d + m0 + 0xeaa127fau) + ((b ^ c) ^ a), 11);
        c = d + BitOperations.RotateLeft((c + m3 + 0xd4ef3085u) + ((a ^ b) ^ d), 16);
        b = c + BitOperations.RotateLeft((b + m6 + 0x04881d05u) + ((d ^ a) ^ c), 23);
        a = b + BitOperations.RotateLeft((a + m9 + 0xd9d4d039u) + ((c ^ d) ^ b), 4);
        d = a + BitOperations.RotateLeft((d + m12 + 0xe6db99e5u) + ((b ^ c) ^ a), 11);
        c = d + BitOperations.RotateLeft((c + m15 + 0x1fa27cf8u) + ((a ^ b) ^ d), 16);
        b = c + BitOperations.RotateLeft((b + m2 + 0xc4ac5665u) + ((d ^ a) ^ c), 23);

        a = b + BitOperations.RotateLeft((a + m0 + 0xf4292244u) + (c ^ (b | ~d)), 6);
        d = a + BitOperations.RotateLeft((d + m7 + 0x432aff97u) + (b ^ (a | ~c)), 10);
        c = d + BitOperations.RotateLeft((c + m14 + 0xab9423a7u) + (a ^ (d | ~b)), 15);
        b = c + BitOperations.RotateLeft((b + m5 + 0xfc93a039u) + (d ^ (c | ~a)), 21);
        a = b + BitOperations.RotateLeft((a + m12 + 0x655b59c3u) + (c ^ (b | ~d)), 6);
        d = a + BitOperations.RotateLeft((d + m3 + 0x8f0ccc92u) + (b ^ (a | ~c)), 10);
        c = d + BitOperations.RotateLeft((c + m10 + 0xffeff47du) + (a ^ (d | ~b)), 15);
        b = c + BitOperations.RotateLeft((b + m1 + 0x85845dd1u) + (d ^ (c | ~a)), 21);
        a = b + BitOperations.RotateLeft((a + m8 + 0x6fa87e4fu) + (c ^ (b | ~d)), 6);
        d = a + BitOperations.RotateLeft((d + m15 + 0xfe2ce6e0u) + (b ^ (a | ~c)), 10);
        c = d + BitOperations.RotateLeft((c + m6 + 0xa3014314u) + (a ^ (d | ~b)), 15);
        b = c + BitOperations.RotateLeft((b + m13 + 0x4e0811a1u) + (d ^ (c | ~a)), 21);
        a = b + BitOperations.RotateLeft((a + m4 + 0xf7537e82u) + (c ^ (b | ~d)), 6);
        d = a + BitOperations.RotateLeft((d + m11 + 0xbd3af235u) + (b ^ (a | ~c)), 10);
        c = d + BitOperations.RotateLeft((c + m2 + 0x2ad7d2bbu) + (a ^ (d | ~b)), 15);
        b = c + BitOperations.RotateLeft((b + m9 + 0xeb86d391u) + (d ^ (c | ~a)), 21);

        state.A += a;
        state.B += b;
        state.C += c;
        state.D += d;
    }

    private struct State
    {
        public uint A;
        public uint B;
        public uint C;
        public uint D;

        public static State Create()
            => new() { A = 0x67452301u, B = 0xefcdab89u, C = 0x98badcfeu, D = 0x10325476u };
    }
}
#endif
