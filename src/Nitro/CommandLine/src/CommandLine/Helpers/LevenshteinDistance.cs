using System.Buffers;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class LevenshteinDistance
{
    private const int StackallocThreshold = 256;

    /// <summary>
    /// Returns the case-insensitive Levenshtein distance between <paramref name="left"/> and <paramref name="right"/>.
    /// </summary>
    public static int Calculate(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        if (left.Length > right.Length)
        {
            var swap = left;
            left = right;
            right = swap;
        }

        var rowLength = left.Length + 1;
        var bufferLength = rowLength * 2;
        int[]? rented = null;
        var buffer =
            bufferLength <= StackallocThreshold
                ? stackalloc int[bufferLength]
                : rented = ArrayPool<int>.Shared.Rent(bufferLength);

        try
        {
            var previous = buffer[..rowLength];
            var current = buffer.Slice(rowLength, rowLength);

            for (var i = 0; i < rowLength; i++)
            {
                previous[i] = i;
            }

            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                current[0] = rightIndex;
                var rightCharacter = char.ToUpperInvariant(right[rightIndex - 1]);

                for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
                {
                    var cost = char.ToUpperInvariant(left[leftIndex - 1]) == rightCharacter ? 0 : 1;
                    current[leftIndex] = Math.Min(
                        Math.Min(current[leftIndex - 1] + 1, previous[leftIndex] + 1),
                        previous[leftIndex - 1] + cost);
                }

                var completed = current;
                current = previous;
                previous = completed;
            }

            return previous[left.Length];
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<int>.Shared.Return(rented);
            }
        }
    }
}
