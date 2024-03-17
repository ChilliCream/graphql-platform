using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution;

/// <summary>
/// Provides the base implementation for a executable task.
/// </summary>
/// <remarks>
/// The task is by default a parallel execution task.
/// </remarks>
public abstract class ExecutionTask : IExecutionTask
{
    private ExecutionTaskStatus _completionStatus = ExecutionTaskStatus.Completed;
    private Task? _task;

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
    public void BeginExecute(CancellationToken cancellationToken)
    {
        Status = ExecutionTaskStatus.Running;
        _task = ExecuteInternalAsync(cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public Task WaitForCompletionAsync(CancellationToken cancellationToken)
        => _task ?? Task.CompletedTask;

    private async ValueTask ExecuteInternalAsync(CancellationToken cancellationToken)
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

        Status = _completionStatus;
        Context.Completed(this);
    }

    /// <summary>
    /// This execute method represents the work of this task.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    protected abstract ValueTask ExecuteAsync(CancellationToken cancellationToken);

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
