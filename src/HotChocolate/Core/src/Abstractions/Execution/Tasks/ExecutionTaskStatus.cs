namespace HotChocolate.Execution;

/// <summary>
/// The execution task status.
/// </summary>
public enum ExecutionTaskStatus
{
    /// <summary>
    /// The task is initialized and waiting to run.
    /// </summary>
    WaitingToRun,

    /// <summary>
    /// The task is running.
    /// </summary>
    Running,

    /// <summary>
    /// The task is completed.
    /// </summary>
    Completed,

    /// <summary>
    /// The task completed and is in a faulted state.
    /// </summary>
    Faulted,
}
