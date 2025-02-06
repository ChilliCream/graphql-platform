namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a GraphQL subscription instance within the execution engine.
/// </summary>
public interface ISubscription
{
    /// <summary>
    /// Gets the internal subscription ID.
    /// </summary>
    ulong Id { get; }

    /// <summary>
    /// Gets the compiled subscription operation.
    /// </summary>
    IOperation Operation { get; }

    /// <summary>
    /// Gets the global request state.
    /// </summary>
    IDictionary<string, object?> ContextData { get; }
}
