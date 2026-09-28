using System.Buffers;
using System.Buffers.Text;
using System.Collections.Immutable;
using System.Text;
using GreenDonut.Data.Internal;

namespace GreenDonut.Data.Cursors;

/// <summary>
/// The cursor parser allows to parser the cursor into its key values.
/// </summary>
public static class CursorParser
{
    private const byte Escape = (byte)'\\';
    private const byte Separator = (byte)':';

    /// <summary>
    /// Parses the cursor into its key values.
    /// </summary>
    /// <param name="cursor">
    /// The cursor that should be parsed.
    /// </param>
    /// <param name="keys">
    /// The keys that make up the cursor.
    /// </param>
    /// <returns>
    /// Returns the key values of the cursor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// If <paramref name="cursor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// If the number of keys is zero.
    /// </exception>
    public static Cursor Parse(string cursor, ReadOnlySpan<CursorKey> keys)
    {
        ArgumentNullException.ThrowIfNull(cursor);

        if (keys.Length == 0)
        {
            throw new ArgumentException("The number of keys must be greater than zero.", nameof(keys));
        }

        var buffer = ArrayPool<byte>.Shared.Rent(cursor.Length * 4);
        var bufferSpan = buffer.AsSpan();
        var length = Encoding.UTF8.GetBytes(cursor, bufferSpan);
        Base64.DecodeFromUtf8InPlace(bufferSpan[..length], out var written);

        if (bufferSpan.Length > written)
        {
            bufferSpan = bufferSpan[..written];
        }

        var (offset, page, totalCount, isEndCursor) = ParsePageInfo(ref bufferSpan);

        if (isEndCursor)
        {
            if (bufferSpan.Length != 0)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw ThrowHelper.CursorParser_PageInfoCouldNotBeParsed();
            }

            ArrayPool<byte>.Shared.Return(buffer);
            return new Cursor([], offset, null, totalCount, IsEndCursor: true);
        }

        var key = 0;
        var start = 0;
        var end = 0;
        var parsedCursor = new object?[keys.Length];

        for (var current = 0; current < bufferSpan.Length; current++)
        {
            var code = bufferSpan[current];
            end++;

            if (CanParse(code, current, bufferSpan))
            {
                if (key >= keys.Length)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    throw new ArgumentException("The number of keys must match the number of values.", nameof(cursor));
                }

                if (code == Separator)
                {
                    end--;
                }

                var span = bufferSpan.Slice(start, end);
                parsedCursor[key] = keys[key].Parse(span);
                start = current + 1;
                end = 0;
                key++;
            }
        }

        ArrayPool<byte>.Shared.Return(buffer);
        return new Cursor(parsedCursor.ToImmutableArray(), offset, page, totalCount);

        static bool CanParse(byte code, int pos, ReadOnlySpan<byte> buffer)
        {
            if (code == Separator)
            {
                if (pos == 0)
                {
                    return true;
                }

                if (buffer[pos - 1] != Escape)
                {
                    return true;
                }
            }

            if (pos == buffer.Length - 1)
            {
                return true;
            }

            return false;
        }
    }

    private static (int? Offset, int? PageIndex, int? TotalCount, bool IsEndCursor) ParsePageInfo(
        ref Span<byte> span)
    {
        const byte open = (byte)'{';
        const byte close = (byte)'}';
        const byte separator = (byte)'|';
        var endPrefix = "end|"u8;

        // Validate input: must start with `{` and end with `}`
        if (span.Length < 2 || span[0] != open)
        {
            return (null, null, null, false);
        }

        // the page info is empty
        if (span[0] == open && span[1] == close)
        {
            span = span[2..];
            return (null, null, null, false);
        }

        // Advance span beyond opening `{`
        span = span[1..];

        if (span.StartsWith(endPrefix))
        {
            span = span[endPrefix.Length..];

            var endSeparatorIndex = ExpectSeparator(span, separator);
            var offsetPart = span[..endSeparatorIndex];
            ParseNumber(offsetPart, out var endOffset, out var endOffsetConsumed);
            var endStart = endSeparatorIndex + 1;

            endSeparatorIndex = ExpectSeparator(span[endStart..], close);
            var totalCountPart = span.Slice(endStart, endSeparatorIndex);
            ParseNumber(totalCountPart, out var endTotalCount, out var endTotalCountConsumed);
            endStart += endSeparatorIndex + 1;

            if (endOffset > 0
                || endTotalCount < 0
                || endOffsetConsumed != offsetPart.Length
                || endTotalCountConsumed != totalCountPart.Length)
            {
                throw ThrowHelper.CursorParser_PageInfoCouldNotBeParsed();
            }

            // Advance span beyond closing `}`
            span = span[endStart..];

            return (endOffset, null, endTotalCount, true);
        }

        var separatorIndex = ExpectSeparator(span, separator);
        var part = span[..separatorIndex];
        ParseNumber(part, out var offset, out _);
        var start = separatorIndex + 1;

        separatorIndex = ExpectSeparator(span[start..], separator);
        part = span.Slice(start, separatorIndex);
        ParseNumber(part, out var page, out _);
        start += separatorIndex + 1;

        separatorIndex = ExpectSeparator(span[start..], close);
        part = span.Slice(start, separatorIndex);
        ParseNumber(part, out var totalCount, out _);
        start += separatorIndex + 1;

        // Advance span beyond closing `}`
        span = span[start..];

        var (resultOffset, resultPage, resultTotalCount) = new CursorPageInfo(offset, page, totalCount);
        return (resultOffset, resultPage, resultTotalCount, false);

        static void ParseNumber(ReadOnlySpan<byte> span, out int value, out int consumed)
        {
            if (!Utf8Parser.TryParse(span, out value, out consumed))
            {
                throw new InvalidOperationException(
                    "The cursor page info could not be parsed.");
            }
        }

        static int ExpectSeparator(ReadOnlySpan<byte> span, byte separator)
        {
            var index = span.IndexOf(separator);

            if (index == -1)
            {
                throw new InvalidOperationException(
                    "The cursor page info could not be parsed.");
            }

            return index;
        }
    }
}
