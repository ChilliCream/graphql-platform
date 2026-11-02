namespace HotChocolate.Execution;

/// <summary>
/// Limits the number of GraphQL executions that can run simultaneously against a
/// single <see cref="IRequestExecutor"/>. The gate is shared by the request pipeline
/// (for queries, mutations, and subscription handshakes) and the subscription event
/// loop (for each inbound event), so that one in-flight execution equals one slot
/// regardless of transport.
/// </summary>
public sealed class ExecutionConcurrencyGate
{
    private readonly SemaphoreSlim? _semaphore;

    /// <summary>
    /// Initializes a new instance of <see cref="ExecutionConcurrencyGate"/>.
    /// </summary>
    /// <param name="maxConcurrentExecutions">
    /// The maximum number of concurrent executions allowed. A value of <c>null</c>
    /// or <c>&lt;= 0</c> disables the gate.
    /// </param>
    public ExecutionConcurrencyGate(int? maxConcurrentExecutions)
    {
        if (maxConcurrentExecutions is { } max and > 0)
        {
            _semaphore = new SemaphoreSlim(max, max);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the gate is enabled.
    /// </summary>
    public bool IsEnabled => _semaphore is not null;

    /// <summary>
    /// Asynchronously waits for a slot to become available.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token to observe while waiting.
    /// </param>
    public ValueTask WaitAsync(CancellationToken cancellationToken)
    {
        if (_semaphore is null)
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(_semaphore.WaitAsync(cancellationToken));
    }

    /// <summary>
    /// Releases a previously acquired slot.
    /// </summary>
    public void Release() => _semaphore?.Release();
}
