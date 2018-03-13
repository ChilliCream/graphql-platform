using System.Collections.Generic;

namespace Prometheus.Abstractions
{
    public interface ISelectionSet
        : IReadOnlyCollection<ISelection>
    {
        string ToString(int indentationDepth);
    }
}