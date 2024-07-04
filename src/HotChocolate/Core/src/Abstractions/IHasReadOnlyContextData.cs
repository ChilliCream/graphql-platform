using System.Collections.Generic;

namespace HotChocolate;

public interface IHasReadOnlyContextData
{
    /// <summary>
    /// The context data dictionary can be used by middleware components and
    /// resolvers to retrieve data during execution.
    /// </summary>
    IReadOnlyDictionary<string, object?> ContextData { get; }
}
