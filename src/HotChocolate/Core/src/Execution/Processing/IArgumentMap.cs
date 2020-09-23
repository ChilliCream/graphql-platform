using System.Collections.Generic;

namespace HotChocolate.Execution.Processing
{
    public interface IArgumentMap
        : IReadOnlyDictionary<NameString, ArgumentValue>
    {
        bool IsFinal { get; }

        bool HasErrors { get; }
    }
}
