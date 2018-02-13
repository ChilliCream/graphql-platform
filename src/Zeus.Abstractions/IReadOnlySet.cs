using System.Collections.Generic;

namespace Zeus.Abstractions
{
    public interface IReadOnlySet<T>
        : IReadOnlyCollection<T>
        , IEnumerable<T>
    {
        bool Contains(T item);
    }
}