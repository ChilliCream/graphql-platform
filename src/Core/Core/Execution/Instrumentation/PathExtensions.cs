using System.Collections.Generic;

namespace HotChocolate.Execution.Instrumentation
{
    internal static class PathExtensions
    {
        public static IReadOnlyCollection<object> ToFieldPathArray(
            this Path path)
        {
            var stack = new Stack<object>();
            Path current = path;

            while (current != null)
            {
                if (current.IsIndexer)
                {
                    stack.Push(current.Index);
                }

                stack.Push(current.Name);
                current = current.Parent;
            }

            return stack;
        }
    }
}
