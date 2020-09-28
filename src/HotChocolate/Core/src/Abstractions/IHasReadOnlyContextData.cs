using System.Collections.Generic;

#nullable enable

namespace HotChocolate
{
    public interface IHasReadOnlyContextData
    {
        /// <summary>
        /// The context data dictionary can be used by middleware components and
        /// resolvers to store and retrieve data during execution.
        /// </summary>
        IReadOnlyDictionary<string, object?> ContextData { get; }
    }
}
