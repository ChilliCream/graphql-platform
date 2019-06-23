using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public interface IStack<T>
        : IEnumerable<T>
        , ICollection
        , IReadOnlyList<T>
    {
        void Push(T value);
        T Peek();
        T Pop();
        bool TryPeek(out T result);
        bool TryPop(out T result);
    }
}
