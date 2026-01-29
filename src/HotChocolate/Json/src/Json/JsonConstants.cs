namespace HotChocolate.Text.Json;

internal static class JsonConstants
{
    public const int StackallocByteThreshold = 256;
    public const int StackallocCharThreshold = StackallocByteThreshold / 2;

    public const int MaxEscapedTokenSize = 1_000_000_000;   // Max size for already escaped value.
    public const int MaxUnescapedTokenSize = MaxEscapedTokenSize / MaxExpansionFactorWhileEscaping;  // 166_666_666 bytes
    public const int MaxCharacterTokenSize = MaxEscapedTokenSize / MaxExpansionFactorWhileTranscoding;  // 333_333_333 chars

    public const byte OpenBrace = (byte)'{';
    public const byte CloseBrace = (byte)'}';
    public const byte OpenBracket = (byte)'[';
    public const byte CloseBracket = (byte)']';
    public const byte Space = (byte)' ';
    public const byte CarriageReturn = (byte)'\r';
    public const byte LineFeed = (byte)'\n';
    public const byte Tab = (byte)'\t';
    public const byte Comma = (byte)',';
    public const byte Quote = (byte)'"';
    public const byte BackSlash = (byte)'\\';
    public const byte Slash = (byte)'/';
    public const byte BackSpace = (byte)'\b';
    public const byte FormFeed = (byte)'\f';
    public const byte Colon = (byte)':';
    public const byte NewLineLineFeed = (byte)'\n';

    public static ReadOnlySpan<byte> TrueValue => "true"u8;
    public static ReadOnlySpan<byte> FalseValue => "false"u8;
    public static ReadOnlySpan<byte> NullValue => "null"u8;

    // In the worst case, an ASCII character represented as a single utf-8 byte could expand 6x when escaped.
    // For example: '+' becomes '\u0043'
    // Escaping surrogate pairs (represented by 3 or 4 utf-8 bytes) would expand to 12 bytes (which is still <= 6x).
    // The same factor applies to utf-16 characters.
    public const int MaxExpansionFactorWhileEscaping = 6;

    // In the worst case, a single UTF-16 character could be expanded to 3 UTF-8 bytes.
    // Only surrogate pairs expand to 4 UTF-8 bytes but that is a transformation of 2 UTF-16 characters going to 4 UTF-8 bytes (factor of 2).
    // All other UTF-16 characters can be represented by either 1 or 2 UTF-8 bytes.
    public const int MaxExpansionFactorWhileTranscoding = 3;

    public const int UnicodePlane01StartValue = 0x10000;
    public const int HighSurrogateStartValue = 0xD800;
    public const int HighSurrogateEndValue = 0xDBFF;
    public const int LowSurrogateStartValue = 0xDC00;
    public const int LowSurrogateEndValue = 0xDFFF;
    public const int BitShiftBy10 = 0x400;

    public const int RemoveFlagsBitMask = 0x7FFFFFFF;
}
