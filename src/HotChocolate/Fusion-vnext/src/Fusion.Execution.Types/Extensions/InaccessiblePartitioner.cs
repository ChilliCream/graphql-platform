namespace HotChocolate.Fusion.Types.Collections;

internal static class InaccessiblePartitioner
{
    public static void PartitionByAccessibility<T>(this T[] array, out int length)
    where T : IInaccessibleProvider
    {
        if (array.Length <= 1)
        {
            length = 0;
            return;
        }

        var writeIndex = 0;

        // Move all accessible items to the front, preserving order
        for (var i = 0; i < array.Length; i++)
        {
            if (!array[i].IsInaccessible)
            {
                if (i != writeIndex)
                {
                    // Swap
                    (array[i], array[writeIndex]) = (array[writeIndex], array[i]);
                }
                writeIndex++;
            }
        }

        length = writeIndex;
    }
}
