namespace HotChocolate.Execution.Processing;

/// <summary>
/// The task statistics keep track of the work in the execution engine.
/// </summary>
internal interface ITaskStatistics
{
    /// <summary>
    /// Signals that the stats have been updated.
    /// </summary>
    event EventHandler<EventArgs> StateChanged;

    /// <summary>
    /// Signals that all task have been completed.
    /// </summary>
    event EventHandler<EventArgs>? AllTasksCompleted;

    /// <summary>
    /// Gets the amount of new tasks that are ready to be processed.
    /// </summary>
    int NewTasks { get; }

    /// <summary>
    /// Gets the amount of running tasks.
    /// </summary>
    int RunningTasks { get; }

    /// <summary>
    /// Gets the amount all tasks known to the execution engine.
    /// </summary>
    int AllTasks { get; }

    /// <summary>
    /// Gets the amount of completed tasks.
    /// </summary>
    int CompletedTasks { get; }

    /// <summary>
    /// Defines if the execution is completed.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Signals to the execution engine that a new task was registered.
    /// </summary>
    void TaskCreated();

    /// <summary>
    /// Signals to the execution engine that a new task was started.
    /// </summary>
    void TaskStarted();

    /// <summary>
    /// Signals to the execution engine that a task was completed.
    /// </summary>
    void TaskCompleted();
}
