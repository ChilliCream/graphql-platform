namespace HotChocolate.Execution;

/// <summary>
/// This interface represents a task that can be executed by the execution engine.
/// </summary>
public interface IExecutionTask
{
    /// <summary>
    /// Gets or sets the internal execution identifier.
    /// </summary>
    uint Id { get; set; }

    /// <summary>
    /// Gets the execution branch id.
    /// </summary>
    int BranchId { get; }

    /// <summary>
    /// Defines the kind of task.
    /// The task kind is used to apply the correct execution strategy.
    /// </summary>
    ExecutionTaskKind Kind { get; }

    /// <summary>
    /// Specifies the status of this task.
    /// </summary>
    ExecutionTaskStatus Status { get; }

    /// <summary>
    /// Next and previous are properties that are used by the execution engine to
    /// track the execution state.
    /// </summary>
    IExecutionTask? Next { get; set; }

    /// <summary>
    /// Next and previous are properties that are used by the execution engine to
    /// track the execution state.
    /// </summary>
    IExecutionTask? Previous { get; set; }

    /// <summary>
    /// This property is set by the execution engine and stores the execution state on it.
    /// </summary>
    object? State { get; set; }

    /// <summary>
    /// This property is set by the execution engine defines if the task needs to be
    /// executed in a serial context.
    /// </summary>
    bool IsSerial { get; set; }

    /// <summary>
    /// Gets a value indicating whether this task is deprioritized.
    /// </summary>
    bool IsDeferred { get; }

    /// <summary>
    /// Specifies if the task was fully registered with the scheduler.
    /// </summary>
    bool IsRegistered { get; set; }

    /// <summary>
    /// Begins executing this task.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    void BeginExecute(CancellationToken cancellationToken);
}
