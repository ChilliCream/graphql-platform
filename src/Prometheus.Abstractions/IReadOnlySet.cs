using System.Collections.Generic;

namespace Prometheus.Abstractions
{
    public interface IReadOnlySet<T>
        : IReadOnlyCollection<T>
        , IEnumerable<T>
    {
        bool Contains(T item);
    }
}