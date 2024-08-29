using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Utilities;

public static class QueueExtensions
{
    public static bool TryPeekElement<T>(
        this Queue<T> queue,
        [NotNullWhen(true)] out T value)
    {
        if (queue == null)
        {
            throw new ArgumentNullException(nameof(queue));
        }

        if (queue.Count > 0)
        {
            value = queue.Peek()!;
            return true;
        }

        value = default!;
        return false;
    }
}
