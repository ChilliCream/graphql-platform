using System.Security.Cryptography;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Generates and validates globally collision-resistant memory and journal
/// entry ids. Each id is a ULID: a 48-bit UTC millisecond timestamp
/// followed by 80 bits of cryptographic randomness, Crockford base32
/// encoded to 26 lowercase characters, so ids sort lexicographically by
/// creation time without a central allocator or a uniqueness check against
/// existing records.
/// </summary>
internal static class MemoryId
{
    public const int Length = 26;

    // Crockford base32: excludes I, L, O, and U to avoid confusion with
    // 1, 1, 0, and V. Already in ascending order, so lexicographic string
    // comparison of encoded ids matches numeric comparison of their value.
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
    /// Derives an id from the first 16 bytes of the given hash, using the
    /// same Crockford base32 encoding <see cref="New(TimeProvider)"/> uses, but without a
    /// timestamp/entropy split: identical input bytes always yield an
    /// identical id.
    /// </summary>
    public static string FromHash(ReadOnlySpan<byte> hash) => Encode(hash[..16]);

    /// <summary>
    /// True when the value is a well-formed id: exactly <see cref="Length"/>
    /// lowercase Crockford base32 characters. Does not check that the id is
    /// actually in use anywhere.
    /// </summary>
    /// <summary>
    /// Returns the id unchanged, or throws when it is not a well-formed
    /// memory id. Guards the one place an id is inlined into SQL rather than
    /// parameterized (see <c>MemoryStore.LoadCuratedAsync</c>), so an id
    /// that never came from this store cannot reach the statement text.
    /// </summary>
    public static string Require(string value)
        => IsValid(value) ? value : throw new ExitException($"Invalid memory id '{value}'.");

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
        Span<char> result = stackalloc char[Length];

        // Timestamp: 48 bits -> 10 characters.
        result[0] = Alphabet[(data[0] & 224) >> 5];
        result[1] = Alphabet[data[0] & 31];
        result[2] = Alphabet[(data[1] & 248) >> 3];
        result[3] = Alphabet[((data[1] & 7) << 2) | ((data[2] & 192) >> 6)];
        result[4] = Alphabet[(data[2] & 62) >> 1];
        result[5] = Alphabet[((data[2] & 1) << 4) | ((data[3] & 240) >> 4)];
        result[6] = Alphabet[((data[3] & 15) << 1) | ((data[4] & 128) >> 7)];
        result[7] = Alphabet[(data[4] & 124) >> 2];
        result[8] = Alphabet[((data[4] & 3) << 3) | ((data[5] & 224) >> 5)];
        result[9] = Alphabet[data[5] & 31];

        // Randomness: 80 bits -> 16 characters.
        result[10] = Alphabet[(data[6] & 248) >> 3];
        result[11] = Alphabet[((data[6] & 7) << 2) | ((data[7] & 192) >> 6)];
        result[12] = Alphabet[(data[7] & 62) >> 1];
        result[13] = Alphabet[((data[7] & 1) << 4) | ((data[8] & 240) >> 4)];
        result[14] = Alphabet[((data[8] & 15) << 1) | ((data[9] & 128) >> 7)];
        result[15] = Alphabet[(data[9] & 124) >> 2];
        result[16] = Alphabet[((data[9] & 3) << 3) | ((data[10] & 224) >> 5)];
        result[17] = Alphabet[data[10] & 31];
        result[18] = Alphabet[(data[11] & 248) >> 3];
        result[19] = Alphabet[((data[11] & 7) << 2) | ((data[12] & 192) >> 6)];
        result[20] = Alphabet[(data[12] & 62) >> 1];
        result[21] = Alphabet[((data[12] & 1) << 4) | ((data[13] & 240) >> 4)];
        result[22] = Alphabet[((data[13] & 15) << 1) | ((data[14] & 128) >> 7)];
        result[23] = Alphabet[(data[14] & 124) >> 2];
        result[24] = Alphabet[((data[14] & 3) << 3) | ((data[15] & 224) >> 5)];
        result[25] = Alphabet[data[15] & 31];

        return new string(result);
    }
}
