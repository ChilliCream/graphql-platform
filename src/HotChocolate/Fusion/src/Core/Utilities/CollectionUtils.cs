using System.Buffers;

namespace HotChocolate.Fusion.Planning;

internal static class CollectionUtils
{
    public static string[] CopyToArray(IReadOnlyList<string> list)
    {
        if (list.Count == 0)
        {
            return Array.Empty<string>();
        }

        var array = new string[list.Count];

        for (var i = 0; i < list.Count; i++)
        {
            array[i] = list[i];
        }

        return array;
    }

    public static string[] CopyToArray(IEnumerable<string> enumerable, ref string[]? buffer, ref int usedBufferCapacity)
    {
        using var enumerator = enumerable.GetEnumerator();
        var next = enumerator.MoveNext();

        // if the enumerable holds no elements we will use Array.Empty to
        // not allocate empty arrays for every empty enumeration.
        if (!next)
        {
            return Array.Empty<string>();
        }

        // now that we know we have items we will allocate our temp
        // list to temporarily store the elements on.
        buffer ??= ArrayPool<string>.Shared.Rent(64);
        var capacity = buffer.Length;
        var index = 0;

        while (next)
        {
            if (capacity <= buffer.Length)
            {
                var temp = ArrayPool<string>.Shared.Rent(capacity * 2);
                Buffer.BlockCopy(buffer, 0, temp, 0, index);
                ArrayPool<string>.Shared.Return(buffer, true);
                buffer = temp;
            }

            buffer[index++] = enumerator.Current;
            next = enumerator.MoveNext();
        }

        // now lets copy the stuff into its array
        var array = new string[buffer.Length];
        Buffer.BlockCopy(buffer, 0, array, 0, index);

        if (array.Length > usedBufferCapacity)
        {
            usedBufferCapacity = array.Length;
        }

        return array;
    }
}