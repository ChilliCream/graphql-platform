using System.Collections.Generic;

namespace HotChocolate.Types.Filters
{
    public class FilterScope<T>
    {
        public FilterScope()
        {
            Level = new Stack<Queue<T>>();
            Instance = new Stack<T>();
            Level.Push(new Queue<T>());
        }

        public Stack<Queue<T>> Level { get; }

        public Stack<T> Instance { get; }
    }
}
