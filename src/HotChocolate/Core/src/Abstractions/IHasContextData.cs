using System.Collections.Generic;

#nullable enable

namespace HotChocolate
{
    public interface IHasContextData
    {
        /// <summary>
        /// The context data dictionary can be used by middleware components and
        /// resolvers to store and retrieve data during execution.
        /// </summary>
        IDictionary<string, object?> ContextData { get; }
    }
}
