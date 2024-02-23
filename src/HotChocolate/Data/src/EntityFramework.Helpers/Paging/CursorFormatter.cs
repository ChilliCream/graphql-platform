using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace HotChocolate.Data;

internal static class CursorFormatter
{
    public static string Format<T>(T item, DataSetKey[] keys)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (keys == null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        if (keys.Length == 0)
        {
            throw new ArgumentException("The number of keys must be greater than zero.", nameof(keys));
        }

        Span<byte> span = stackalloc byte[256];
        byte[]? poolArray = null;

        var totalWritten = 0;
        var first = true;

        foreach (var key in keys)
        {
            if (!first)
            {
                if (totalWritten + 1 > span.Length)
                {
                    ExpandBuffer(ref poolArray, ref span, totalWritten, 1);
                }
                span[totalWritten++] = (byte)':';
            }
            else
            {
                first = false;
            }

            if (!key.TryFormat(item, span[totalWritten..], out var written))
            {
                ExpandBuffer(ref poolArray, ref span, totalWritten, written);

                if (!key.TryFormat(item, span[totalWritten..], out written))
                {
                    throw new InvalidOperationException();
                }
            }
            totalWritten += written;
        }

        var maxNeededSpace = Base64.GetMaxEncodedToUtf8Length(totalWritten);

        if (maxNeededSpace > span.Length)
        {
            ExpandBuffer(ref poolArray, ref span, totalWritten, maxNeededSpace - totalWritten);
        }

        if (Base64.EncodeToUtf8InPlace(span, totalWritten, out var bytesWritten) != OperationStatus.Done)
        {
            throw new InvalidOperationException("The input is not a valid UTF-8 string.");
        }

        var result = Encoding.UTF8.GetString(span.Slice(0, bytesWritten));

        if (poolArray != null)
        {
            ArrayPool<byte>.Shared.Return(poolArray);
        }

        return result;
    }

    private static void ExpandBuffer(
        ref byte[]? poolArray,
        ref Span<byte> span,
        int currentLength,
        int additionalLength)
    {
        var newSize = poolArray == null ? 256 : poolArray.Length * 2;
        while (newSize < currentLength + additionalLength)
        {
            newSize *= 2;
        }

        var newPoolArray = ArrayPool<byte>.Shared.Rent(newSize);
        span[..currentLength].CopyTo(newPoolArray);

        if (poolArray != null)
        {
            ArrayPool<byte>.Shared.Return(poolArray);
        }

        poolArray = newPoolArray;
        span = new Span<byte>(poolArray);
    }
}