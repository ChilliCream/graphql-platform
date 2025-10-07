namespace HotChocolate.Execution.Relay;

/// <summary>
/// Specifies the encoding format used for GraphQL Global ID serialization.
/// </summary>
public enum NodeIdSerializerFormat
{
    /// <summary>
    /// Standard Base64 encoding using the characters A-Z, a-z, 0-9, +, /, and = for padding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses the standard Base64 alphabet as defined in RFC 4648. This format provides
    /// excellent size efficiency (33% overhead) and fast encoding/decoding performance.
    /// </para>
    /// </remarks>
    Base64,

    /// <summary>
    /// URL-safe Base64 encoding using A-Z, a-z, 0-9, -, _, with padding removed or replaced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses URL-safe Base64 alphabet where + becomes -, / becomes _, and padding
    /// is handled appropriately for URL contexts. This is the recommended format
    /// for web applications and APIs.
    /// </para>
    /// </remarks>
    UrlSafeBase64,

    /// <summary>
    /// Mathematical Base36 encoding using digits 0-9 and letters A-Z (case-insensitive).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses Base36 encoding which treats the entire byte array as a single big-endian
    /// number and converts it to base 36. This format preserves trailing zeros and
    /// provides case-insensitive parsing.
    /// </para>
    /// </remarks>
    Base36,

    /// <summary>
    /// Uppercase hexadecimal encoding using characters 0-9 and A-F.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Standard hexadecimal representation where each byte becomes two uppercase
    /// hex characters. This format provides maximum human readability and is
    /// useful for debugging and development scenarios.
    /// </para>
    /// </remarks>
    UpperHex,

    /// <summary>
    /// Lowercase hexadecimal encoding using characters 0-9 and a-f.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hexadecimal representation using lowercase letters. Functionally identical
    /// to <see cref="UpperHex"/> but uses lowercase a-f instead of A-F. Choice
    /// between upper and lower case is typically based on convention preferences.
    /// </para>
    /// </remarks>
    LowerHex
}
