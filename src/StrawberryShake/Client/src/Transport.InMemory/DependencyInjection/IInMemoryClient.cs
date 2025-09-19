using HotChocolate.Execution;

namespace StrawberryShake.Transport.InMemory;

/// <summary>
/// Represents a client for sending and receiving messaged to a local schema
/// </summary>
public interface IInMemoryClient
{
    /// <summary>
    /// The name of the schema that should be used by this executor
    /// </summary>
    string SchemaName { get; set; }

    /// <summary>
    /// The request executor that will be used to execute the request
    /// </summary>
    IRequestExecutor? Executor { get; set; }

    /// <summary>
    /// A list of interceptors that can be used to intercept requests before they are executed
    /// </summary>
    IList<IInMemoryRequestInterceptor> RequestInterceptors { get; }

    /// <summary>
    /// The name of the socket
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Sends data asynchronously to to the local schema.
    /// </summary>
    /// <param name="request">The request to execute</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation</param>
    /// <returns>The result of the operation</returns>
    ValueTask<IExecutionResult> ExecuteAsync(
        OperationRequest request,
        CancellationToken cancellationToken = default);
}
