using System.Buffers;

namespace HotChocolate.Buffers;

internal static class BufferPools
{
    private static readonly ArrayPool<byte> s_veryLarge = ArrayPool<byte>.Create(
        maxArrayLength: 16 * 1024 * 1024,
        maxArraysPerBucket: 2);
    private static readonly ArrayPool<byte> s_large = ArrayPool<byte>.Create(
        maxArrayLength: 8 * 1024 * 1024,
        maxArraysPerBucket: 10);
    private static readonly ArrayPool<byte> s_standard = ArrayPool<byte>.Shared;

    public static byte[] Rent(int minimumSize)
    {
        if (minimumSize <= 1 * 1024 * 1024)
        {
            return s_standard.Rent(minimumSize);
        }

        if(minimumSize <= 8 * 1024 * 1024)
        {
            return s_large.Rent(minimumSize);
        }

        return s_veryLarge.Rent(minimumSize);
    }

    public static void Return(byte[] array, bool clearArray = false)
    {
        var length = array.Length;

        if (length <= 1 * 1024 * 1024)
        {
            s_standard.Return(array, clearArray);
        }
        else if (length <= 8 * 1024 * 1024)
        {
            s_large.Return(array, clearArray);
        }
        else
        {
            s_veryLarge.Return(array, clearArray);
        }
    }
}
