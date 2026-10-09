using System.Text;

namespace HotChocolate.Fusion.Types.Collections;

/// <summary>
/// Looks up items by their UTF-8 encoded GraphQL name.
/// </summary>
internal static class Utf8NameIndex
{
    // Sorted by length ascending then lexicographic byte order so the span
    // lookup can binary search with a length-first comparison.
    private static readonly Comparer<byte[]> s_comparer =
        Comparer<byte[]>.Create(static (a, b) => CompareLengthThenBytes(a, b));

    /// <summary>
    /// Creates the UTF-8 names of the first <paramref name="length"/> items, sorted for <see cref="IndexOf"/>.
    /// </summary>
    public static byte[][] Create<T>(T[] items, int length, Func<T, string> getName)
    {
        var utf8Names = Encode(items, length, getName);
        Array.Sort(utf8Names, s_comparer);
        return utf8Names;
    }

    /// <summary>
    /// Creates the UTF-8 names of the first <paramref name="length"/> items, sorted for <see cref="IndexOf"/>,
    /// and returns the items in the same order through <paramref name="sortedItems"/>.
    /// </summary>
    public static byte[][] Create<T>(T[] items, int length, Func<T, string> getName, out T[] sortedItems)
    {
        var utf8Names = Encode(items, length, getName);
        sortedItems = items[..length];
        Array.Sort(utf8Names, sortedItems, s_comparer);
        return utf8Names;
    }

    /// <summary>
    /// Gets the index of <paramref name="utf8Name"/> in names created by this index,
    /// or -1 if it is not contained.
    /// </summary>
    public static int IndexOf(byte[][] utf8Names, ReadOnlySpan<byte> utf8Name)
    {
        var count = utf8Names.Length;

        if (count <= 8)
        {
            for (var i = 0; i < count; i++)
            {
                if (utf8Name.SequenceEqual(utf8Names[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        var low = 0;
        var high = count - 1;

        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var comparison = CompareLengthThenBytes(utf8Names[mid], utf8Name);

            if (comparison == 0)
            {
                return mid;
            }

            if (comparison < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return -1;
    }

    private static byte[][] Encode<T>(T[] items, int length, Func<T, string> getName)
    {
        var utf8Names = new byte[length][];

        for (var i = 0; i < length; i++)
        {
            utf8Names[i] = Encoding.UTF8.GetBytes(getName(items[i]));
        }

        return utf8Names;
    }

    private static int CompareLengthThenBytes(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        var lengthComparison = x.Length - y.Length;

        if (lengthComparison != 0)
        {
            return lengthComparison;
        }

        return x.SequenceCompareTo(y);
    }
}
