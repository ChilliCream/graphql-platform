using System.Buffers;
using System.Buffers.Text;
using System.Collections.Immutable;
using System.Text;

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

        var key = 0;
        var start = 0;
        var end = 0;
        var parsedCursor = new object?[keys.Length];
        var (offset, page, totalCount) = ParsePageInfo(ref bufferSpan);

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

    private static CursorPageInfo ParsePageInfo(ref Span<byte> span)
    {
        const byte Open = (byte)'{';
        const byte Close = (byte)'}';
        const byte Separator = (byte)'|';

        // Validate input: must start with `{` and end with `}`
        if (span.Length < 2 || span[0] != Open)
        {
            return default;
        }

        // the page info is empty
        if (span[0] == Open && span[1] == Close)
        {
            span = span[2..];
            return default;
        }

        // Advance span beyond opening `{`
        span = span[1..];

        var separatorIndex = ExpectSeparator(span, Separator);
        var part = span[..separatorIndex];
        ParseNumber(part, out var offset, out var consumed);
        var start = separatorIndex + 1;

        separatorIndex = ExpectSeparator(span[start..], Separator);
        part = span.Slice(start, separatorIndex);
        ParseNumber(part, out var page, out consumed);
        start += separatorIndex + 1;

        separatorIndex = ExpectSeparator(span[start..], Close);
        part = span.Slice(start, separatorIndex);
        ParseNumber(part, out var totalCount, out consumed);
        start += separatorIndex + 1;

        // Advance span beyond closing `}`
        span = span[start..];

        return new CursorPageInfo(offset, page, totalCount);

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
