namespace HotChocolate.Execution;

/// <summary>
/// Represents a request to execute a GraphQL operation.
/// </summary>
public interface IExecutionRequest
{
    /// <summary>
    /// Gets the initial request state.
    /// </summary>
    IReadOnlyDictionary<string, object?>? ContextData { get; }

    /// <summary>
    /// Gets the services that shall be used while executing the GraphQL request.
    /// </summary>
    IServiceProvider? Services { get; }
}
