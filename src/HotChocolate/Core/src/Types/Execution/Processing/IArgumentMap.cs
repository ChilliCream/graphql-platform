#nullable enable

using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Processing;

public interface IArgumentMap : IReadOnlyDictionary<string, ArgumentValue>
{
    bool IsFinalNoErrors { get; }

    bool IsFinal { get; }

    bool HasErrors { get; }
}
