using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace HotChocolate.Data;

internal static class CursorParser
{
    private const byte _separator = (byte)':';
    
    public static object[] Parse(string cursor, ReadOnlySpan<DataSetKey> keys)
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
        var parsedCursor = new object[keys.Length];
        for(var current = 0; current < bufferSpan.Length; current++)
        {
            var code = bufferSpan[current];
            end++;

            if (code == _separator || current == bufferSpan.Length - 1)
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
    }
}