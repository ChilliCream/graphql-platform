namespace HotChocolate.Fetching;

/// <summary>
/// Represents the type of event that can occur within the <see cref="IBatchDispatcher"/>.
/// </summary>
public enum BatchDispatchEventType
{
    /// <summary>
    /// A batch was enqueued.
    /// </summary>
    Enqueued,

    /// <summary>
    /// A batch was evaluated.
    /// </summary>
    Evaluated,

    /// <summary>
    /// A batch was dispatched.
    /// </summary>
    Dispatched,

    /// <summary>
    /// The coordination task was started.
    /// </summary>
    CoordinatorStarted,

    /// <summary>
    /// The coordination task was completed
    /// </summary>
    CoordinatorCompleted
}
