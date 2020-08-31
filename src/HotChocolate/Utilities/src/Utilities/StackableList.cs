using System;
using System.Collections.Generic;

namespace HotChocolate.Utilities
{
    public class StackableList<T> : List<T>, IStackableList<T>
    {
        public T Pop()
        {
            int lastIndex = Count - 1;
            T p = this[lastIndex];
            RemoveAt(lastIndex);
            return p;
        }

        public T Peek() => PeekAt(0);

        public T PeekAt(int index)
        {
            int lastIndex = Count - 1 - index;
            return this[lastIndex];
        }

        public bool TryPeek(out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }

            item = Peek();
            return true;
        }

        public bool TryPeekAt(int index, out T item)
        {
            if (Count <= index)
            {
                item = default;
                return false;
            }

            item = PeekAt(index);
            return true;
        }

        public void Push(T item)
        {
            Add(item);
        }
    }
}