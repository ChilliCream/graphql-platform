namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Schedules the execution of source schema requests.
/// Execution nodes call this interface instead of <see cref="ISourceSchemaClient"/> directly,
/// allowing the scheduler to hold requests that belong to the same batching group until all
/// group members have submitted or been skipped, and then dispatch them as a single batch.
/// </summary>
public interface ISourceSchemaScheduler
{
    /// <summary>
    /// Submits a request for execution. If the request belongs to a batching group,
    /// the returned task may not complete until all other members of the group have
    /// submitted or been skipped.
    /// </summary>
    /// <param name="context">The current operation plan execution context.</param>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response from the source schema.</returns>
    ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken);
}
