using System.Collections.Generic;

namespace HotChocolate.Utilities
{
    public static class QueueExtensions
    {
        public static bool TryPeekElement<T>(
            this Queue<T> queue,
            out T value)
        {
            if (queue.Count > 0)
            {
                value = queue.Peek();
                return true;
            }

            value = default;
            return false;
        }
    }
}
