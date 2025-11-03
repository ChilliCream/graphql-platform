namespace HotChocolate;

public interface IHasReadOnlyContextData
{
    /// <summary>
    /// Gets the context data dictionary that can be used by middleware components and
    /// resolvers to retrieve data during execution.
    /// </summary>
    IReadOnlyDictionary<string, object?> ContextData { get; }
}
