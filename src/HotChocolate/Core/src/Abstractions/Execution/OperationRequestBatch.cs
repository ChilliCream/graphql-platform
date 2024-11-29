namespace HotChocolate.Execution;

/// <summary>
/// Represents a batch of GraphQL requests that shall be executed together.
/// </summary>
/// <param name="requests">
/// The requests within this batch.
/// </param>
/// <param name="contextData">
/// The initial request state.
/// </param>
/// <param name="services">
/// The services that shall be used while executing the GraphQL request.
/// </param>
public sealed class OperationRequestBatch(
    IReadOnlyList<IOperationRequest> requests,
    IReadOnlyDictionary<string, object?>? contextData = null,
    IServiceProvider? services = null)
    : IExecutionRequest
{
    /// <summary>
    /// The requests within this batch.
    /// </summary>
    public IReadOnlyList<IOperationRequest> Requests { get; } = requests;

    /// <summary>
    /// Gets the initial request state.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? ContextData { get; } = contextData;

    /// <summary>
    /// Gets the services that shall be used while executing the GraphQL request.
    /// </summary>
    public IServiceProvider? Services { get; } = services;
}
