using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace HotChocolate.Pagination.Expressions;

/// <summary>
/// The cursor parser allows to parser the cursor into its key values.
/// </summary>
public static class CursorParser
{
    private const byte _escape = (byte)':';
    private const byte _separator = (byte)':';

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
    public static object?[] Parse(string cursor, ReadOnlySpan<CursorKey> keys)
    {
        if (cursor == null)
        {
            throw new ArgumentNullException(nameof(cursor));
        }

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
        for(var current = 0; current < bufferSpan.Length; current++)
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

                if (code == _separator)
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
        return parsedCursor;

        static bool CanParse(byte code, int pos, ReadOnlySpan<byte> buffer)
        {
            if (code == _separator)
            {
                if (pos == 0)
                {
                    return true;
                }

                if (buffer[pos - 1] != _escape)
                {
                    return true;
                }
            }

            if(pos == buffer.Length - 1)
            {
                return true;
            }

            return false;
        }
    }
}
