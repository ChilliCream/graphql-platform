using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Processing;

public interface IArgumentMap : IReadOnlyDictionary<NameString, ArgumentValue>
{
    bool IsFinalNoErrors { get; }

    bool IsFinal { get; }

    bool HasErrors { get; }
}
