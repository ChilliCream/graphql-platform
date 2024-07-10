using System.Collections.Generic;

namespace HotChocolate;

/// <summary>
/// Defines that the implementor of this interface allows to
/// access and store arbitrary context data.
/// </summary>
public interface IHasContextData
{
    /// <summary>
    /// The context data dictionary can be used by middleware components and
    /// resolvers to store and retrieve data during execution.
    /// </summary>
    IDictionary<string, object?> ContextData { get; }
}
