using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IOperation
    {
        string Name { get; }

        IDocument Document { get; }

        IReadOnlyDictionary<string, object> Variables { get; }
    }
}
