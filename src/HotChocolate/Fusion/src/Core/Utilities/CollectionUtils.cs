using System.Buffers;

namespace HotChocolate.Fusion.Utilities;

internal static class CollectionUtils
{
    public static string[] CopyToArray(IReadOnlyList<string> list)
    {
        if (list.Count == 0)
        {
            return [];
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
            return [];
        }

        // now that we know we have items we will allocate our temp
        // list to temporarily store the elements on.
        buffer ??= ArrayPool<string>.Shared.Rent(64);
        var capacity = buffer.Length;
        var index = 0;

        while (next)
        {
            if (capacity <= index)
            {
                var temp = ArrayPool<string>.Shared.Rent(capacity * 2);
                buffer.AsSpan(0, index).CopyTo(temp);
                ArrayPool<string>.Shared.Return(buffer, true);
                buffer = temp;
                capacity = buffer.Length;
            }

            buffer[index++] = enumerator.Current;
            next = enumerator.MoveNext();
        }

        // now lets copy the stuff into its array
        var array = new string[index];
        buffer.AsSpan(0, index).CopyTo(array);

        if (array.Length > usedBufferCapacity)
        {
            usedBufferCapacity = array.Length;
        }

        return array;
    }
}
