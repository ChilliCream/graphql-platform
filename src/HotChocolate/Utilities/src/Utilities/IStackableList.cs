using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Utilities
{
    public interface IStackableList<T>
        : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
    {
        T Peek();

        T PeekAt(int index);

        T Pop();

        void Push(T item);

        bool TryPeek(out T item);

        bool TryPeekAt(int index, out T item);
    }
}
