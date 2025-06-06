namespace HotChocolate.Execution;

// Leave it as IRequestExecutor
/// <summary>
/// This executor processes GraphQL query, mutation and subscription requests against an underlying schema.
/// </summary>
public interface IRequestExecutor
{
    /// <summary>
    /// Gets an <see cref="ulong"/> representing the version of the executor.
    /// When a schema is updated and a new executor is created, the version is incremented.
    /// </summary>
    ulong Version { get; }

    /// <summary>
    /// Gets the schema definition that this executor operates on.
    /// </summary>
    ISchemaDefinition Schema { get; }

    /// <summary>
    /// Executes the given GraphQL <paramref name="request" />.
    /// </summary>
    /// <param name="request">
    /// The GraphQL request object.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the execution result of the given GraphQL <paramref name="request" />.
    ///
    /// If the request operation is a simple query or mutation the result is a
    /// <see cref="IOperationResult" />.
    ///
    /// If the request operation is a query or mutation where data is deferred, streamed or
    /// includes live data the result is a <see cref="IResponseStream" /> where each result
    /// that the <see cref="IResponseStream" /> yields is a <see cref="IOperationResult" />.
    ///
    /// If the request operation is a subscription the result is a
    /// <see cref="IResponseStream" /> where each result that the
    /// <see cref="IResponseStream" /> yields is a
    /// <see cref="IOperationResult" />.
    /// </returns>
    Task<IExecutionResult> ExecuteAsync(
        IOperationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the given GraphQL <paramref name="requestBatch" />.
    /// </summary>
    /// <param name="requestBatch">
    /// The GraphQL request batch.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a stream of query results.
    /// </returns>
    Task<IResponseStream> ExecuteBatchAsync(
        OperationRequestBatch requestBatch,
        CancellationToken cancellationToken = default);
}
