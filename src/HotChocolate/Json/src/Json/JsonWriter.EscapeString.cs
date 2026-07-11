using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;

namespace HotChocolate.Text.Json;

public sealed partial class JsonWriter
{
    // Only allow ASCII characters between ' ' (0x20) and '~' (0x7E), inclusively,
    // but exclude characters that need to be escaped as hex: '"', '\'', '&', '+', '<', '>', '`'
    // and exclude characters that need to be escaped by adding a backslash: '\n', '\r', '\t', '\\', '\b', '\f'
    //
    // non-zero = allowed, 0 = disallowed
    private const int LastAsciiCharacter = 0x7F;

    private static ReadOnlySpan<byte> AllowList => // byte.MaxValue + 1
    [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+0000..U+000F
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+0010..U+001F
        1, 1, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1, // U+0020..U+002F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, // U+0030..U+003F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // U+0040..U+004F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, // U+0050..U+005F
        0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // U+0060..U+006F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, // U+0070..U+007F

        // Also include the ranges from U+0080 to U+00FF for performance to avoid UTF8 code from checking boundary.
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 // U+00F0..U+00FF
    ];

    private const string HexFormatString = "X4";
    private static readonly StandardFormat s_hexStandardFormat = new('X', 4);

    private static bool NeedsEscaping(byte value) => AllowList[value] == 0;

    private static bool NeedsEscapingNoBoundsCheck(char value) => AllowList[value] == 0;

    private static int NeedsEscaping(ReadOnlySpan<byte> value, JavaScriptEncoder? encoder)
        => (encoder ?? JavaScriptEncoder.Default).FindFirstCharacterToEncodeUtf8(value);

    private static int NeedsEscaping(ReadOnlySpan<char> value, JavaScriptEncoder? encoder)
    {
        // Some implementations of JavaScriptEncoder.FindFirstCharacterToEncode may not accept
        // null pointers and guard against that. Hence, check up-front to return -1.
        if (value.IsEmpty)
        {
            return -1;
        }

        // Unfortunately, there is no public API for FindFirstCharacterToEncode(Span<char>) yet,
        // so we have to use the unsafe FindFirstCharacterToEncode(char*, int) instead.
        unsafe
        {
            fixed (char* ptr = value)
            {
                return (encoder ?? JavaScriptEncoder.Default).FindFirstCharacterToEncode(ptr, value.Length);
            }
        }
    }

    private static int GetMaxEscapedLength(int textLength, int firstIndexToEscape)
    {
        Debug.Assert(textLength > 0);
        Debug.Assert(firstIndexToEscape >= 0 && firstIndexToEscape < textLength);
        return firstIndexToEscape + (JsonConstants.MaxExpansionFactorWhileEscaping * (textLength - firstIndexToEscape));
    }

    private static void EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, JavaScriptEncoder encoder, ref int consumed, ref int written, bool isFinalBlock)
    {
        Debug.Assert(encoder != null);

        var result = encoder.EncodeUtf8(value, destination, out var encoderBytesConsumed, out var encoderBytesWritten, isFinalBlock);

        Debug.Assert(result != OperationStatus.DestinationTooSmall);
        Debug.Assert(result != OperationStatus.NeedMoreData || !isFinalBlock);

        if (!(result == OperationStatus.Done || (result == OperationStatus.NeedMoreData && !isFinalBlock)))
        {
            throw CreateArgumentException_InvalidUTF8(value[encoderBytesWritten..]);
        }

        Debug.Assert(encoderBytesConsumed == value.Length || (result == OperationStatus.NeedMoreData && !isFinalBlock));

        written += encoderBytesWritten;
        consumed += encoderBytesConsumed;
    }

    private static void EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, int indexOfFirstByteToEscape, JavaScriptEncoder? encoder, out int written)
        => EscapeString(value, destination, indexOfFirstByteToEscape, encoder, out _, out written, isFinalBlock: true);

    private static void EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, int indexOfFirstByteToEscape, JavaScriptEncoder? encoder, out int consumed, out int written, bool isFinalBlock = true)
    {
        Debug.Assert(indexOfFirstByteToEscape >= 0 && indexOfFirstByteToEscape < value.Length);

        value[..indexOfFirstByteToEscape].CopyTo(destination);
        written = indexOfFirstByteToEscape;
        consumed = indexOfFirstByteToEscape;

        if (encoder != null)
        {
            destination = destination[indexOfFirstByteToEscape..];
            value = value[indexOfFirstByteToEscape..];
            EscapeString(value, destination, encoder, ref consumed, ref written, isFinalBlock);
        }
        else
        {
            // For performance when no encoder is specified, perform escaping here for Ascii and on the
            // first occurrence of a non-Ascii character, then call into the default encoder.
            while (indexOfFirstByteToEscape < value.Length)
            {
                var val = value[indexOfFirstByteToEscape];
                if (IsAsciiValue(val))
                {
                    if (NeedsEscaping(val))
                    {
                        EscapeNextBytes(val, destination, ref written);
                        indexOfFirstByteToEscape++;
                        consumed++;
                    }
                    else
                    {
                        destination[written] = val;
                        written++;
                        indexOfFirstByteToEscape++;
                        consumed++;
                    }
                }
                else
                {
                    // Fall back to default encoder.
                    destination = destination[written..];
                    value = value[indexOfFirstByteToEscape..];
                    EscapeString(value, destination, JavaScriptEncoder.Default, ref consumed, ref written, isFinalBlock);
                    break;
                }
            }
        }
    }

    private static void EscapeNextBytes(byte value, Span<byte> destination, ref int written)
    {
        destination[written++] = (byte)'\\';
        switch (value)
        {
            case JsonConstants.Quote:
                // Optimize for the common quote case.
                destination[written++] = (byte)'u';
                destination[written++] = (byte)'0';
                destination[written++] = (byte)'0';
                destination[written++] = (byte)'2';
                destination[written++] = (byte)'2';
                break;
            case JsonConstants.LineFeed:
                destination[written++] = (byte)'n';
                break;
            case JsonConstants.CarriageReturn:
                destination[written++] = (byte)'r';
                break;
            case JsonConstants.Tab:
                destination[written++] = (byte)'t';
                break;
            case JsonConstants.BackSlash:
                destination[written++] = (byte)'\\';
                break;
            case JsonConstants.BackSpace:
                destination[written++] = (byte)'b';
                break;
            case JsonConstants.FormFeed:
                destination[written++] = (byte)'f';
                break;
            default:
                destination[written++] = (byte)'u';
                var result = Utf8Formatter.TryFormat(value, destination[written..], out var bytesWritten, format: s_hexStandardFormat);
                Debug.Assert(result);
                Debug.Assert(bytesWritten == 4);
                written += bytesWritten;
                break;
        }
    }

    private static bool IsAsciiValue(byte value) => value <= LastAsciiCharacter;

    private static bool IsAsciiValue(char value) => value <= LastAsciiCharacter;

    private static void EscapeString(ReadOnlySpan<char> value, Span<char> destination, JavaScriptEncoder encoder, ref int consumed, ref int written, bool isFinalBlock)
    {
        Debug.Assert(encoder != null);

        var result = encoder.Encode(value, destination, out var encoderBytesConsumed, out var encoderCharsWritten, isFinalBlock);

        Debug.Assert(result != OperationStatus.DestinationTooSmall);
        Debug.Assert(result != OperationStatus.NeedMoreData || !isFinalBlock);

        if (!(result == OperationStatus.Done || (result == OperationStatus.NeedMoreData && !isFinalBlock)))
        {
            throw new ArgumentException(string.Format(
                "Cannot encode invalid UTF-16 text as JSON. Invalid surrogate value: '{0}'.",
                value[encoderCharsWritten]));
        }

        Debug.Assert(encoderBytesConsumed == value.Length || (result == OperationStatus.NeedMoreData && !isFinalBlock));

        written += encoderCharsWritten;
        consumed += encoderBytesConsumed;
    }

    private static void EscapeString(ReadOnlySpan<char> value, Span<char> destination, int indexOfFirstByteToEscape, JavaScriptEncoder? encoder, out int written)
        => EscapeString(value, destination, indexOfFirstByteToEscape, encoder, out _, out written, isFinalBlock: true);

    private static void EscapeString(ReadOnlySpan<char> value, Span<char> destination, int indexOfFirstByteToEscape, JavaScriptEncoder? encoder, out int consumed, out int written, bool isFinalBlock = true)
    {
        Debug.Assert(indexOfFirstByteToEscape >= 0 && indexOfFirstByteToEscape < value.Length);

        value[..indexOfFirstByteToEscape].CopyTo(destination);
        written = indexOfFirstByteToEscape;
        consumed = indexOfFirstByteToEscape;

        if (encoder != null)
        {
            destination = destination[indexOfFirstByteToEscape..];
            value = value[indexOfFirstByteToEscape..];
            EscapeString(value, destination, encoder, ref consumed, ref written, isFinalBlock);
        }
        else
        {
            // For performance when no encoder is specified, perform escaping here for Ascii and on the
            // first occurrence of a non-Ascii character, then call into the default encoder.
            while (indexOfFirstByteToEscape < value.Length)
            {
                var val = value[indexOfFirstByteToEscape];
                if (IsAsciiValue(val))
                {
                    if (NeedsEscapingNoBoundsCheck(val))
                    {
                        EscapeNextChars(val, destination, ref written);
                        indexOfFirstByteToEscape++;
                        consumed++;
                    }
                    else
                    {
                        destination[written] = val;
                        written++;
                        indexOfFirstByteToEscape++;
                        consumed++;
                    }
                }
                else
                {
                    // Fall back to default encoder.
                    destination = destination[written..];
                    value = value[indexOfFirstByteToEscape..];
                    EscapeString(value, destination, JavaScriptEncoder.Default, ref consumed, ref written, isFinalBlock);
                    break;
                }
            }
        }
    }

    private static void EscapeNextChars(char value, Span<char> destination, ref int written)
    {
        Debug.Assert(IsAsciiValue(value));

        destination[written++] = '\\';
        switch ((byte)value)
        {
            case JsonConstants.Quote:
                // Optimize for the common quote case.
                destination[written++] = 'u';
                destination[written++] = '0';
                destination[written++] = '0';
                destination[written++] = '2';
                destination[written++] = '2';
                break;
            case JsonConstants.LineFeed:
                destination[written++] = 'n';
                break;
            case JsonConstants.CarriageReturn:
                destination[written++] = 'r';
                break;
            case JsonConstants.Tab:
                destination[written++] = 't';
                break;
            case JsonConstants.BackSlash:
                destination[written++] = '\\';
                break;
            case JsonConstants.BackSpace:
                destination[written++] = 'b';
                break;
            case JsonConstants.FormFeed:
                destination[written++] = 'f';
                break;
            default:
                destination[written++] = 'u';
                int intChar = value;
                intChar.TryFormat(destination[written..], out var charsWritten, HexFormatString);
                Debug.Assert(charsWritten == 4);
                written += charsWritten;
                break;
        }
    }

    public static ArgumentException CreateArgumentException_InvalidUTF8(ReadOnlySpan<byte> value)
    {
        var builder = new StringBuilder();
        builder.Append("Cannot encode invalid UTF-8 text as JSON. Invalid input: '");

        var printFirst10 = Math.Min(value.Length, 10);

        for (var i = 0; i < printFirst10; i++)
        {
            var nextByte = value[i];
            if (IsPrintable(nextByte))
            {
                builder.Append((char)nextByte);
            }
            else
            {
                builder.Append($"0x{nextByte:X2}");
            }
        }

        if (printFirst10 < value.Length)
        {
            builder.Append("...");
        }

        builder.Append("'.");

        return new ArgumentException(builder.ToString());
    }

    private static bool IsPrintable(byte value) => value >= 0x20 && value < 0x7F;
}
