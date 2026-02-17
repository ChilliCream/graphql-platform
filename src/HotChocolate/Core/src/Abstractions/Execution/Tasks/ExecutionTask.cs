namespace HotChocolate.Execution;

/// <summary>
/// Provides the base implementation for an executable task.
/// </summary>
/// <remarks>
/// The task is by default a parallel execution task.
/// </remarks>
public abstract class ExecutionTask : IExecutionTask
{
    private ExecutionTaskStatus _completionStatus = ExecutionTaskStatus.Completed;
    private Task? _task;

    /// <summary>
    /// Gets or sets the internal execution identifier.
    /// </summary>
    public uint Id { get; set; }

    /// <inheritdoc />
    public abstract int BranchId { get; }

    /// <summary>
    /// Gets the execution engine task context.
    /// </summary>
    protected abstract IExecutionTaskContext Context { get; }

    /// <inheritdoc />
    public virtual ExecutionTaskKind Kind => ExecutionTaskKind.Parallel;

    /// <inheritdoc />
    public ExecutionTaskStatus Status { get; private set; }

    /// <inheritdoc />
    public IExecutionTask? Next { get; set; }

    /// <inheritdoc />
    public IExecutionTask? Previous { get; set; }

    /// <inheritdoc />
    public object? State { get; set; }

    /// <inheritdoc />
    public bool IsSerial { get; set; }

    /// <inheritdoc />
    public bool IsRegistered { get; set; }

    /// <inheritdoc />
    public abstract bool IsDeferred { get; }

    /// <inheritdoc />
    public void BeginExecute(CancellationToken cancellationToken)
    {
        Status = ExecutionTaskStatus.Running;
        _task = ExecuteInternalAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task WaitForCompletionAsync(CancellationToken cancellationToken)
        => _task ?? Task.CompletedTask;

    private async Task ExecuteInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (Context.Track(this))
            {
                await ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            Faulted();

            // If we run into this exception the request was aborted.
            // In this case we do nothing and just return.
        }
        catch (Exception ex)
        {
            Faulted();

            if (!cancellationToken.IsCancellationRequested)
            {
                Context.ReportError(this, ex);
            }
        }
        finally
        {
            Status = _completionStatus;
            Context.Completed(this);

            await OnAfterCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// This execute method represents the work of this task.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    protected abstract ValueTask ExecuteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Called after the task has completed, regardless of whether it succeeded or faulted.
    /// Override this method to perform post-completion logic such as cleanup or notifications.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    protected virtual ValueTask OnAfterCompletedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <summary>
    /// Completes the task as faulted.
    /// </summary>
    protected void Faulted()
    {
        _completionStatus = ExecutionTaskStatus.Faulted;
    }

    /// <summary>
    /// Resets the state of this task in case the task object is reused.
    /// </summary>
    protected void Reset()
    {
        _task = null;
        Next = null;
        Previous = null;
        State = null;
        IsSerial = false;
        IsRegistered = false;
        _completionStatus = ExecutionTaskStatus.Completed;
        Status = ExecutionTaskStatus.WaitingToRun;
    }
}
