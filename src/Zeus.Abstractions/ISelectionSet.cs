using System.Collections.Generic;

namespace Zeus.Abstractions
{
    public interface ISelectionSet
        : IReadOnlyCollection<ISelection>
    {
        string ToString(int indentationDepth);
    }
}