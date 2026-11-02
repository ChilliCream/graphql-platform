using System.Security.Cryptography;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Generates lowercase, 26-character memory ids from a millisecond timestamp and
/// random data, or from a hash. Timestamp-based ids sort by their encoded timestamp.
/// </summary>
internal static class MemoryId
{
    public const int Length = 26;

    // Lowercase Crockford base32 alphabet in ascending order.
    private const string Alphabet = "0123456789abcdefghjkmnpqrstvwxyz";

    public static string New(TimeProvider timeProvider) => New(timeProvider.GetUtcNow());

    public static string New(DateTimeOffset timestamp)
    {
        Span<byte> bytes = stackalloc byte[16];
        var millis = (ulong)timestamp.ToUnixTimeMilliseconds();

        bytes[0] = (byte)(millis >> 40);
        bytes[1] = (byte)(millis >> 32);
        bytes[2] = (byte)(millis >> 24);
        bytes[3] = (byte)(millis >> 16);
        bytes[4] = (byte)(millis >> 8);
        bytes[5] = (byte)millis;

        RandomNumberGenerator.Fill(bytes[6..]);

        return Encode(bytes);
    }

    /// <summary>
    /// Encodes the first 16 bytes of the hash as a lowercase memory id.
    /// Identical bytes produce identical ids; fewer than 16 bytes are invalid.
    /// </summary>
    public static string FromHash(ReadOnlySpan<byte> hash) => Encode(hash[..16]);

    /// <summary>
    /// Returns a syntactically valid memory id unchanged, or throws <see cref="ExitException"/>.
    /// </summary>
    public static string Require(string value)
        => IsValid(value) ? value : throw new ExitException($"Invalid memory id '{value}'.");

    /// <summary>
    /// True for exactly <see cref="Length"/> lowercase Crockford base32 characters.
    /// Does not check whether the id exists.
    /// </summary>
    public static bool IsValid(string value)
    {
        if (value.Length != Length)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (Alphabet.IndexOf(c) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private static string Encode(ReadOnlySpan<byte> data)
    {
        Span<char> result =
        [
            // First 48 bits, encoded as 10 characters.
            Alphabet[(data[0] & 224) >> 5],
            Alphabet[data[0] & 31],
            Alphabet[(data[1] & 248) >> 3],
            Alphabet[((data[1] & 7) << 2) | ((data[2] & 192) >> 6)],
            Alphabet[(data[2] & 62) >> 1],
            Alphabet[((data[2] & 1) << 4) | ((data[3] & 240) >> 4)],
            Alphabet[((data[3] & 15) << 1) | ((data[4] & 128) >> 7)],
            Alphabet[(data[4] & 124) >> 2],
            Alphabet[((data[4] & 3) << 3) | ((data[5] & 224) >> 5)],
            Alphabet[data[5] & 31],
            // Remaining 80 bits, encoded as 16 characters.
            Alphabet[(data[6] & 248) >> 3],
            Alphabet[((data[6] & 7) << 2) | ((data[7] & 192) >> 6)],
            Alphabet[(data[7] & 62) >> 1],
            Alphabet[((data[7] & 1) << 4) | ((data[8] & 240) >> 4)],
            Alphabet[((data[8] & 15) << 1) | ((data[9] & 128) >> 7)],
            Alphabet[(data[9] & 124) >> 2],
            Alphabet[((data[9] & 3) << 3) | ((data[10] & 224) >> 5)],
            Alphabet[data[10] & 31],
            Alphabet[(data[11] & 248) >> 3],
            Alphabet[((data[11] & 7) << 2) | ((data[12] & 192) >> 6)],
            Alphabet[(data[12] & 62) >> 1],
            Alphabet[((data[12] & 1) << 4) | ((data[13] & 240) >> 4)],
            Alphabet[((data[13] & 15) << 1) | ((data[14] & 128) >> 7)],
            Alphabet[(data[14] & 124) >> 2],
            Alphabet[((data[14] & 3) << 3) | ((data[15] & 224) >> 5)],
            Alphabet[data[15] & 31]
        ];

        return new string(result);
    }
}
