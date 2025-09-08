using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Utilities;

public static class QueueExtensions
{
    public static bool TryPeekElement<T>(
        this Queue<T> queue,
        [NotNullWhen(true)] out T value)
    {
        ArgumentNullException.ThrowIfNull(queue);

        if (queue.Count > 0)
        {
            value = queue.Peek()!;
            return true;
        }

        value = default!;
        return false;
    }
}
