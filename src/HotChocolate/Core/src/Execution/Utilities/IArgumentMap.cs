using System.Collections.Generic;

namespace HotChocolate.Execution.Utilities
{
    public interface IArgumentMap
        : IReadOnlyDictionary<NameString, ArgumentValue>
    {
        bool IsFinal { get; }

        bool HasErrors { get; }
    }
}
