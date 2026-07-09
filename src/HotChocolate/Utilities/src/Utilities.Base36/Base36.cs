using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace HotChocolate.Buffers.Text;

/// <summary>
/// Provides high-performance Base36 encoding and decoding operations.
/// Base36 uses digits 0-9 and letters A-Z, making it case-insensitive and URL-safe.
/// This implementation is optimized for GraphQL Global IDs with fast paths for common cases.
/// </summary>
public static class Base36
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly Encoding s_utf8 = Encoding.UTF8;

    /// <summary>
    /// Encodes bytes to Base36 using optimized operations
    /// </summary>
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        // Fast path for small inputs (up to 16 bytes)
        if (bytes.Length <= 16)
        {
            return EncodeFastUInt128(bytes);
        }

        // Fallback to BigInteger for larger inputs
        return EncodeBigInteger(bytes);
    }

    /// <summary>
    /// Fast encoding using UInt128 for inputs &gt;= 16 bytes
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string EncodeFastUInt128(ReadOnlySpan<byte> bytes)
    {
        // Convert bytes to UInt128 (big-endian interpretation)
        UInt128 value = 0;
        for (var i = 0; i < bytes.Length; i++)
        {
            value = (value << 8) | bytes[i];
        }

        if (value == 0)
        {
            return "0";
        }

        // Estimate maximum characters needed for UInt128
        // log36(2^128) ≈ 25 characters max
        Span<char> buffer = stackalloc char[25];
        var pos = 0;

        while (value > 0)
        {
            buffer[pos++] = Alphabet[(int)(value % 36)];
            value /= 36;
        }

        // Reverse the buffer
        buffer[..pos].Reverse();
        return new string(buffer[..pos]);
    }

    /// <summary>
    /// Encoding using BigInteger for larger inputs
    /// </summary>
    private static string EncodeBigInteger(ReadOnlySpan<byte> bytes)
    {
        // Create BigInteger with big-endian byte order for consistent interpretation
        var value = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);

        if (value.IsZero)
        {
            return "0";
        }

        // Calculate exact length needed
        var temp = value;
        var length = 0;
        while (temp > 0)
        {
            length++;
            temp /= 36;
        }

        return string.Create(length, value, static (span, val) =>
        {
            var pos = 0;
            var temp = val;

            // Build result in reverse
            while (temp > 0)
            {
                var remainder = (int)(temp % 36);
                span[pos++] = Alphabet[remainder];
                temp /= 36;
            }

            // Reverse the span in place
            span.Reverse();
        });
    }

    /// <summary>
    /// Estimates the maximum number of bytes needed to decode a Base36 string
    /// </summary>
    /// <param name="encoded">The Base36 encoded string</param>
    /// <returns>The maximum number of bytes that could be produced</returns>
    public static int GetByteCount(ReadOnlySpan<char> encoded)
    {
        if (encoded.IsEmpty)
        {
            return 0;
        }

        // Calculate the maximum possible value that can be represented
        // with the given number of Base36 digits: 36^length - 1
        // Then calculate how many bytes are needed to represent that value

        // log2(36^n) = n * log2(36) ≈ n * 5.17
        // Divide by 8 to get bytes, add 1 for ceiling
        var estimatedBits = encoded.Length * 5.17;
        var estimatedBytes = (int)Math.Ceiling(estimatedBits / 8.0);

        // Add safety margin of 1 byte to ensure we never underestimate
        return estimatedBytes + 1;
    }

    /// <summary>
    /// Decodes Base36 string to bytes, writing into the provided output span
    /// </summary>
    /// <param name="encoded">The Base36 encoded string</param>
    /// <returns>A string with the decoded value.</returns>
    public static string Decode(ReadOnlySpan<char> encoded)
    {
        var expectedSize = GetByteCount(encoded);

        byte[]? rentedBuffer = null;
        var buffer = expectedSize <= 256
            ? stackalloc byte[expectedSize]
            : (rentedBuffer = ArrayPool<byte>.Shared.Rent(expectedSize));

        try
        {
            var length = Decode(encoded, buffer);
            return s_utf8.GetString(buffer[..length]);
        }
        finally
        {
            if (rentedBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    /// <summary>
    /// Decodes Base36 string to bytes, writing into the provided output span
    /// </summary>
    /// <param name="encoded">The Base36 encoded string</param>
    /// <param name="output">The span to write the decoded bytes into</param>
    /// <returns>The number of bytes written to the output span</returns>
    public static int Decode(ReadOnlySpan<char> encoded, Span<byte> output)
    {
        if (encoded.IsEmpty)
        {
            return 0;
        }

        // Estimate if we can use the fast path
        // UInt128 can handle up to ~25 Base36 characters
        if (encoded.Length <= 25)
        {
            return DecodeFastUInt128(encoded, output);
        }

        // Fallback to BigInteger for larger inputs
        return DecodeBigInteger(encoded, output);
    }

    /// <summary>
    /// Fast decoding using UInt128 for smaller inputs
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DecodeFastUInt128(ReadOnlySpan<char> encoded, Span<byte> output)
    {
        UInt128 value = 0;

        // Process from left to right (standard positional notation)
        for (var i = 0; i < encoded.Length; i++)
        {
            var digit = GetDigitValue(encoded[i]);
            value = value * 36 + (UInt128)digit;
        }

        if (value == 0)
        {
            if (output.Length < 1)
            {
                throw new ArgumentException("Output buffer too small");
            }
            output[0] = 0;
            return 1;
        }

        // Convert UInt128 to bytes (big-endian output)
        var bytesWritten = 0;

        // Find the number of significant bytes
        var temp = value;
        var significantBytes = 0;
        while (temp > 0)
        {
            significantBytes++;
            temp >>= 8;
        }

        if (significantBytes > output.Length)
        {
            throw new ArgumentException($"Output buffer too small. Need {significantBytes} bytes, got {output.Length}");
        }

        // Write bytes in big-endian order
        for (var i = significantBytes - 1; i >= 0; i--)
        {
            output[i] = (byte)(value & 0xFF);
            value >>= 8;
            bytesWritten++;
        }

        return bytesWritten;
    }

    /// <summary>
    /// Decoding using BigInteger for larger inputs
    /// </summary>
    private static int DecodeBigInteger(ReadOnlySpan<char> encoded, Span<byte> output)
    {
        var value = BigInteger.Zero;

        // Process from left to right (standard positional notation)
        for (var i = 0; i < encoded.Length; i++)
        {
            var digit = GetDigitValue(encoded[i]);
            value = value * 36 + digit;
        }

        // Convert to big-endian bytes
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);

        if (bytes.Length > output.Length)
        {
            throw new ArgumentException($"Output buffer too small. Need {bytes.Length} bytes, got {output.Length}");
        }

        bytes.CopyTo(output);
        return bytes.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetDigitValue(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'A' and <= 'Z' => c - 'A' + 10,
            >= 'a' and <= 'z' => c - 'a' + 10,
            _ => throw new ArgumentException($"Invalid Base36 character: {c}")
        };
    }
}
