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

        a = b + BitOperations.RotateLeft(a + ((b & c) | (~b & d)) + m0 + 0xd76aa478u, 7);
        d = a + BitOperations.RotateLeft(d + ((a & b) | (~a & c)) + m1 + 0xe8c7b756u, 12);
        c = d + BitOperations.RotateLeft(c + ((d & a) | (~d & b)) + m2 + 0x242070dbu, 17);
        b = c + BitOperations.RotateLeft(b + ((c & d) | (~c & a)) + m3 + 0xc1bdceeeu, 22);
        a = b + BitOperations.RotateLeft(a + ((b & c) | (~b & d)) + m4 + 0xf57c0fafu, 7);
        d = a + BitOperations.RotateLeft(d + ((a & b) | (~a & c)) + m5 + 0x4787c62au, 12);
        c = d + BitOperations.RotateLeft(c + ((d & a) | (~d & b)) + m6 + 0xa8304613u, 17);
        b = c + BitOperations.RotateLeft(b + ((c & d) | (~c & a)) + m7 + 0xfd469501u, 22);
        a = b + BitOperations.RotateLeft(a + ((b & c) | (~b & d)) + m8 + 0x698098d8u, 7);
        d = a + BitOperations.RotateLeft(d + ((a & b) | (~a & c)) + m9 + 0x8b44f7afu, 12);
        c = d + BitOperations.RotateLeft(c + ((d & a) | (~d & b)) + m10 + 0xffff5bb1u, 17);
        b = c + BitOperations.RotateLeft(b + ((c & d) | (~c & a)) + m11 + 0x895cd7beu, 22);
        a = b + BitOperations.RotateLeft(a + ((b & c) | (~b & d)) + m12 + 0x6b901122u, 7);
        d = a + BitOperations.RotateLeft(d + ((a & b) | (~a & c)) + m13 + 0xfd987193u, 12);
        c = d + BitOperations.RotateLeft(c + ((d & a) | (~d & b)) + m14 + 0xa679438eu, 17);
        b = c + BitOperations.RotateLeft(b + ((c & d) | (~c & a)) + m15 + 0x49b40821u, 22);

        a = b + BitOperations.RotateLeft(a + ((d & b) | (~d & c)) + m1 + 0xf61e2562u, 5);
        d = a + BitOperations.RotateLeft(d + ((c & a) | (~c & b)) + m6 + 0xc040b340u, 9);
        c = d + BitOperations.RotateLeft(c + ((b & d) | (~b & a)) + m11 + 0x265e5a51u, 14);
        b = c + BitOperations.RotateLeft(b + ((a & c) | (~a & d)) + m0 + 0xe9b6c7aau, 20);
        a = b + BitOperations.RotateLeft(a + ((d & b) | (~d & c)) + m5 + 0xd62f105du, 5);
        d = a + BitOperations.RotateLeft(d + ((c & a) | (~c & b)) + m10 + 0x02441453u, 9);
        c = d + BitOperations.RotateLeft(c + ((b & d) | (~b & a)) + m15 + 0xd8a1e681u, 14);
        b = c + BitOperations.RotateLeft(b + ((a & c) | (~a & d)) + m4 + 0xe7d3fbc8u, 20);
        a = b + BitOperations.RotateLeft(a + ((d & b) | (~d & c)) + m9 + 0x21e1cde6u, 5);
        d = a + BitOperations.RotateLeft(d + ((c & a) | (~c & b)) + m14 + 0xc33707d6u, 9);
        c = d + BitOperations.RotateLeft(c + ((b & d) | (~b & a)) + m3 + 0xf4d50d87u, 14);
        b = c + BitOperations.RotateLeft(b + ((a & c) | (~a & d)) + m8 + 0x455a14edu, 20);
        a = b + BitOperations.RotateLeft(a + ((d & b) | (~d & c)) + m13 + 0xa9e3e905u, 5);
        d = a + BitOperations.RotateLeft(d + ((c & a) | (~c & b)) + m2 + 0xfcefa3f8u, 9);
        c = d + BitOperations.RotateLeft(c + ((b & d) | (~b & a)) + m7 + 0x676f02d9u, 14);
        b = c + BitOperations.RotateLeft(b + ((a & c) | (~a & d)) + m12 + 0x8d2a4c8au, 20);

        a = b + BitOperations.RotateLeft(a + (b ^ c ^ d) + m5 + 0xfffa3942u, 4);
        d = a + BitOperations.RotateLeft(d + (a ^ b ^ c) + m8 + 0x8771f681u, 11);
        c = d + BitOperations.RotateLeft(c + (d ^ a ^ b) + m11 + 0x6d9d6122u, 16);
        b = c + BitOperations.RotateLeft(b + (c ^ d ^ a) + m14 + 0xfde5380cu, 23);
        a = b + BitOperations.RotateLeft(a + (b ^ c ^ d) + m1 + 0xa4beea44u, 4);
        d = a + BitOperations.RotateLeft(d + (a ^ b ^ c) + m4 + 0x4bdecfa9u, 11);
        c = d + BitOperations.RotateLeft(c + (d ^ a ^ b) + m7 + 0xf6bb4b60u, 16);
        b = c + BitOperations.RotateLeft(b + (c ^ d ^ a) + m10 + 0xbebfbc70u, 23);
        a = b + BitOperations.RotateLeft(a + (b ^ c ^ d) + m13 + 0x289b7ec6u, 4);
        d = a + BitOperations.RotateLeft(d + (a ^ b ^ c) + m0 + 0xeaa127fau, 11);
        c = d + BitOperations.RotateLeft(c + (d ^ a ^ b) + m3 + 0xd4ef3085u, 16);
        b = c + BitOperations.RotateLeft(b + (c ^ d ^ a) + m6 + 0x04881d05u, 23);
        a = b + BitOperations.RotateLeft(a + (b ^ c ^ d) + m9 + 0xd9d4d039u, 4);
        d = a + BitOperations.RotateLeft(d + (a ^ b ^ c) + m12 + 0xe6db99e5u, 11);
        c = d + BitOperations.RotateLeft(c + (d ^ a ^ b) + m15 + 0x1fa27cf8u, 16);
        b = c + BitOperations.RotateLeft(b + (c ^ d ^ a) + m2 + 0xc4ac5665u, 23);

        a = b + BitOperations.RotateLeft(a + (c ^ (b | ~d)) + m0 + 0xf4292244u, 6);
        d = a + BitOperations.RotateLeft(d + (b ^ (a | ~c)) + m7 + 0x432aff97u, 10);
        c = d + BitOperations.RotateLeft(c + (a ^ (d | ~b)) + m14 + 0xab9423a7u, 15);
        b = c + BitOperations.RotateLeft(b + (d ^ (c | ~a)) + m5 + 0xfc93a039u, 21);
        a = b + BitOperations.RotateLeft(a + (c ^ (b | ~d)) + m12 + 0x655b59c3u, 6);
        d = a + BitOperations.RotateLeft(d + (b ^ (a | ~c)) + m3 + 0x8f0ccc92u, 10);
        c = d + BitOperations.RotateLeft(c + (a ^ (d | ~b)) + m10 + 0xffeff47du, 15);
        b = c + BitOperations.RotateLeft(b + (d ^ (c | ~a)) + m1 + 0x85845dd1u, 21);
        a = b + BitOperations.RotateLeft(a + (c ^ (b | ~d)) + m8 + 0x6fa87e4fu, 6);
        d = a + BitOperations.RotateLeft(d + (b ^ (a | ~c)) + m15 + 0xfe2ce6e0u, 10);
        c = d + BitOperations.RotateLeft(c + (a ^ (d | ~b)) + m6 + 0xa3014314u, 15);
        b = c + BitOperations.RotateLeft(b + (d ^ (c | ~a)) + m13 + 0x4e0811a1u, 21);
        a = b + BitOperations.RotateLeft(a + (c ^ (b | ~d)) + m4 + 0xf7537e82u, 6);
        d = a + BitOperations.RotateLeft(d + (b ^ (a | ~c)) + m11 + 0xbd3af235u, 10);
        c = d + BitOperations.RotateLeft(c + (a ^ (d | ~b)) + m2 + 0x2ad7d2bbu, 15);
        b = c + BitOperations.RotateLeft(b + (d ^ (c | ~a)) + m9 + 0xeb86d391u, 21);

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
