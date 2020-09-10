using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Utilities
{
    public static class StackExtensions
    {
        public static bool TryPeekElement<T>(
            this Stack<T> stack,
            out T value)
        {
            if (stack.Count > 0)
            {
                value = stack.Peek();
                return true;
            }

            value = default;
            return false;
        }
    }
}
