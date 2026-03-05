using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

#if FUSION
using HotChocolate.Text.Json;
using static HotChocolate.Fusion.Properties.FusionExecutionResources;

namespace HotChocolate.Fusion.Text.Json;
#else
using static HotChocolate.Properties.TextJsonResources;

namespace HotChocolate.Text.Json;
#endif

internal static class JsonReaderHelper
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonValueKind ToValueKind(this ElementTokenType tokenType)
    {
        switch (tokenType)
        {
            case ElementTokenType.None:
                return JsonValueKind.Undefined;
            case ElementTokenType.StartArray:
                return JsonValueKind.Array;
            case ElementTokenType.StartObject:
                return JsonValueKind.Object;
            case ElementTokenType.String:
            case ElementTokenType.Number:
            case ElementTokenType.True:
            case ElementTokenType.False:
            case ElementTokenType.Null:
                // This is the offset between the set of literals within JsonValueType and JsonTokenType
                // Essentially: JsonTokenType.Null - JsonValueType.Null
                return (JsonValueKind)((byte)tokenType - 4);
            default:
                Debug.Fail($"No mapping for token type {tokenType}");
                return JsonValueKind.Undefined;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementTokenType ToElementTokenType(this JsonTokenType tokenType)
    {
        switch (tokenType)
        {
            case JsonTokenType.None:
                return ElementTokenType.None;
            case JsonTokenType.StartArray:
                return ElementTokenType.StartArray;
            case JsonTokenType.EndArray:
                return ElementTokenType.EndArray;
            case JsonTokenType.StartObject:
                return ElementTokenType.StartObject;
            case JsonTokenType.EndObject:
                return ElementTokenType.EndObject;
            case JsonTokenType.PropertyName:
                return ElementTokenType.PropertyName;
            case JsonTokenType.Comment:
                return ElementTokenType.Comment;
            case JsonTokenType.String:
                return ElementTokenType.String;
            case JsonTokenType.Number:
                return ElementTokenType.Number;
            case JsonTokenType.True:
                return ElementTokenType.True;
            case JsonTokenType.False:
                return ElementTokenType.False;
            case JsonTokenType.Null:
                return ElementTokenType.Null;
            default:
                throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonValueKind ToValueKind(this JsonTokenType tokenType)
    {
        switch (tokenType)
        {
            case JsonTokenType.None:
                return JsonValueKind.Undefined;
            case JsonTokenType.StartArray:
                return JsonValueKind.Array;
            case JsonTokenType.StartObject:
                return JsonValueKind.Object;
            case JsonTokenType.String:
            case JsonTokenType.Number:
            case JsonTokenType.True:
            case JsonTokenType.False:
            case JsonTokenType.Null:
                // This is the offset between the set of literals within JsonValueType and JsonTokenType
                // Essentially: JsonTokenType.Null - JsonValueType.Null
                return (JsonValueKind)((byte)tokenType - 4);
            default:
                Debug.Fail($"No mapping for token type {tokenType}");
                return JsonValueKind.Undefined;
        }
    }

    public static bool UnescapeAndCompare(ReadOnlySpan<byte> utf8Source, ReadOnlySpan<byte> other)
    {
        Debug.Assert(
            utf8Source.Length >= other.Length
                && utf8Source.Length / JsonConstants.MaxExpansionFactorWhileEscaping <= other.Length);

        byte[]? unescapedArray = null;

        var utf8Unescaped = utf8Source.Length <= JsonConstants.StackallocByteThreshold ?
            stackalloc byte[JsonConstants.StackallocByteThreshold] :
            (unescapedArray = ArrayPool<byte>.Shared.Rent(utf8Source.Length));

        Unescape(utf8Source, utf8Unescaped, 0, out var written);
        Debug.Assert(written > 0);

        utf8Unescaped = utf8Unescaped[..written];
        Debug.Assert(!utf8Unescaped.IsEmpty);

        var result = other.SequenceEqual(utf8Unescaped);

        if (unescapedArray != null)
        {
            utf8Unescaped.Clear();
            ArrayPool<byte>.Shared.Return(unescapedArray);
        }

        return result;
    }

    public static string GetUnescapedString(ReadOnlySpan<byte> utf8Source)
    {
        // The escaped name is always >= than the unescaped, so it is safe to use escaped name for the buffer length.
        var length = utf8Source.Length;
        byte[]? pooledName = null;

        var utf8Unescaped = length <= JsonConstants.StackallocByteThreshold ?
            stackalloc byte[JsonConstants.StackallocByteThreshold] :
            (pooledName = ArrayPool<byte>.Shared.Rent(length));

        Unescape(utf8Source, utf8Unescaped, out var written);
        Debug.Assert(written > 0);

        utf8Unescaped = utf8Unescaped[..written];
        Debug.Assert(!utf8Unescaped.IsEmpty);

        var utf8String = TranscodeHelper(utf8Unescaped);

        if (pooledName != null)
        {
            utf8Unescaped.Clear();
            ArrayPool<byte>.Shared.Return(pooledName);
        }

        return utf8String;
    }

    public static string TranscodeHelper(ReadOnlySpan<byte> utf8Unescaped)
    {
        try
        {
#if NET
            return s_utf8Encoding.GetString(utf8Unescaped);
#else
                if (utf8Unescaped.IsEmpty)
                {
                    return string.Empty;
                }
                unsafe
                {
                    fixed (byte* bytePtr = utf8Unescaped)
                    {
                        return s_utf8Encoding.GetString(bytePtr, utf8Unescaped.Length);
                    }
                }
#endif
        }
        catch (DecoderFallbackException ex)
        {
            // We want to be consistent with the exception being thrown
            // so the user only has to catch a single exception.
            // Since we already throw InvalidOperationException for mismatch token type,
            // and while unescaping, using that exception for failure to decode invalid UTF-8 bytes as well.
            // Therefore, wrapping the DecoderFallbackException around an InvalidOperationException.
            throw new InvalidOperationException(
                JsonReaderHelper_TranscodeHelper_CannotTranscodeInvalidUtf8,
                ex)
            {
                Source = Rethrowable
            };
        }
    }

    internal static void Unescape(ReadOnlySpan<byte> source, Span<byte> destination, int idx, out int written)
    {
        Debug.Assert(idx >= 0 && idx < source.Length);
        Debug.Assert(source[idx] == JsonConstants.BackSlash);
        Debug.Assert(destination.Length >= source.Length);

        var result = TryUnescape(source, destination, idx, out written);
        Debug.Assert(result);
    }

    internal static void Unescape(ReadOnlySpan<byte> source, Span<byte> destination, out int written)
    {
        Debug.Assert(destination.Length >= source.Length);

        var idx = source.IndexOf(JsonConstants.BackSlash);
        Debug.Assert(idx >= 0);

        var result = TryUnescape(source, destination, idx, out written);
        Debug.Assert(result);
    }

    /// <summary>
    /// Used when writing to buffers not guaranteed to fit the unescaped result.
    /// </summary>
    private static bool TryUnescape(ReadOnlySpan<byte> source, Span<byte> destination, int idx, out int written)
    {
        Debug.Assert(idx >= 0 && idx < source.Length);
        Debug.Assert(source[idx] == JsonConstants.BackSlash);

        if (!source[..idx].TryCopyTo(destination))
        {
            written = 0;
            goto DestinationTooShort;
        }

        written = idx;

        while (true)
        {
            Debug.Assert(source[idx] == JsonConstants.BackSlash);

            if (written == destination.Length)
            {
                goto DestinationTooShort;
            }

            switch (source[++idx])
            {
                case JsonConstants.Quote:
                    destination[written++] = JsonConstants.Quote;
                    break;
                case (byte)'n':
                    destination[written++] = JsonConstants.LineFeed;
                    break;
                case (byte)'r':
                    destination[written++] = JsonConstants.CarriageReturn;
                    break;
                case JsonConstants.BackSlash:
                    destination[written++] = JsonConstants.BackSlash;
                    break;
                case JsonConstants.Slash:
                    destination[written++] = JsonConstants.Slash;
                    break;
                case (byte)'t':
                    destination[written++] = JsonConstants.Tab;
                    break;
                case (byte)'b':
                    destination[written++] = JsonConstants.BackSpace;
                    break;
                case (byte)'f':
                    destination[written++] = JsonConstants.FormFeed;
                    break;
                default:
                    Debug.Assert(source[idx] == 'u',
                        "invalid escape sequences must have already been caught by Utf8JsonReader.Read()");

                    // The source is known to be valid JSON, and hence if we see a \u, it is guaranteed to have 4 hex digits following it
                    // Otherwise, the Utf8JsonReader would have already thrown an exception.
                    Debug.Assert(source.Length >= idx + 5);

                    var result = Utf8Parser.TryParse(source.Slice(idx + 1, 4), out int scalar, out var bytesConsumed,
                        'x');
                    Debug.Assert(result);
                    Debug.Assert(bytesConsumed == 4);
                    idx += 4;

                    if (JsonHelpers.IsInRangeInclusive((uint)scalar, JsonConstants.HighSurrogateStartValue,
                        JsonConstants.LowSurrogateEndValue))
                    {
                        // The first hex value cannot be a low surrogate.
                        if (scalar >= JsonConstants.LowSurrogateStartValue)
                        {
                            ThrowHelper.ThrowInvalidOperationException_ReadInvalidUTF16(scalar);
                        }

                        Debug.Assert(JsonHelpers.IsInRangeInclusive((uint)scalar, JsonConstants.HighSurrogateStartValue,
                            JsonConstants.HighSurrogateEndValue));

                        // We must have a low surrogate following a high surrogate.
                        if (source.Length < idx + 7 || source[idx + 1] != '\\' || source[idx + 2] != 'u')
                        {
                            ThrowHelper.ThrowInvalidOperationException_ReadIncompleteUTF16();
                        }

                        // The source is known to be valid JSON, and hence if we see a \u, it is guaranteed to have 4 hex digits following it
                        // Otherwise, the Utf8JsonReader would have already thrown an exception.
                        result = Utf8Parser.TryParse(source.Slice(idx + 3, 4), out int lowSurrogate, out bytesConsumed,
                            'x');
                        Debug.Assert(result);
                        Debug.Assert(bytesConsumed == 4);
                        idx += 6;

                        // If the first hex value is a high surrogate, the next one must be a low surrogate.
                        if (!JsonHelpers.IsInRangeInclusive((uint)lowSurrogate, JsonConstants.LowSurrogateStartValue,
                            JsonConstants.LowSurrogateEndValue))
                        {
                            ThrowHelper.ThrowInvalidOperationException_ReadInvalidUTF16(lowSurrogate);
                        }

                        // To find the unicode scalar:
                        // (0x400 * (High surrogate - 0xD800)) + Low surrogate - 0xDC00 + 0x10000
                        scalar = (JsonConstants.BitShiftBy10 * (scalar - JsonConstants.HighSurrogateStartValue))
                            + (lowSurrogate - JsonConstants.LowSurrogateStartValue)
                            + JsonConstants.UnicodePlane01StartValue;
                    }

                    var rune = new Rune(scalar);
                    var success = rune.TryEncodeToUtf8(destination[written..], out var bytesWritten);
                    if (!success)
                    {
                        goto DestinationTooShort;
                    }

                    Debug.Assert(bytesWritten <= 4);
                    written += bytesWritten;
                    break;
            }

            if (++idx == source.Length)
            {
                goto Success;
            }

            if (source[idx] != JsonConstants.BackSlash)
            {
                var remaining = source[idx..];
                var nextUnescapedSegmentLength = remaining.IndexOf(JsonConstants.BackSlash);
                if (nextUnescapedSegmentLength < 0)
                {
                    nextUnescapedSegmentLength = remaining.Length;
                }

                if ((uint)(written + nextUnescapedSegmentLength) >= (uint)destination.Length)
                {
                    goto DestinationTooShort;
                }

                Debug.Assert(nextUnescapedSegmentLength > 0);
                switch (nextUnescapedSegmentLength)
                {
                    case 1:
                        destination[written++] = source[idx++];
                        break;
                    case 2:
                        destination[written++] = source[idx++];
                        destination[written++] = source[idx++];
                        break;
                    case 3:
                        destination[written++] = source[idx++];
                        destination[written++] = source[idx++];
                        destination[written++] = source[idx++];
                        break;
                    default:
                        remaining[..nextUnescapedSegmentLength].CopyTo(destination[written..]);
                        written += nextUnescapedSegmentLength;
                        idx += nextUnescapedSegmentLength;
                        break;
                }

                Debug.Assert(idx == source.Length || source[idx] == JsonConstants.BackSlash);

                if (idx == source.Length)
                {
                    goto Success;
                }
            }
        }

Success:
        return true;

DestinationTooShort:
        return false;
    }
}
