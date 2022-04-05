namespace HotChocolate.Execution;

/// <summary>
/// Extensions for <see cref="IExecutionTask"/>.
/// </summary>
public static class ExecutionTaskExtensions
{
    /// <summary>
    /// Defines if this task is completed.
    /// </summary>
    public static bool IsCompleted(this IExecutionTask task)
        => task.Status is ExecutionTaskStatus.Completed or ExecutionTaskStatus.Faulted;
}
