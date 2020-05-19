using System.Collections.Generic;

namespace HotChocolate.Execution.Utilities
{
    public interface IPreparedArgumentMap
        : IReadOnlyDictionary<NameString, PreparedArgument>
    {
        bool IsFinal { get; }

        bool HasErrors { get; }
    }
}
