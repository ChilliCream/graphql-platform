namespace HotChocolate.Fetching;

/// <summary>
/// The execution engine batch dispatcher.
/// </summary>
public interface IBatchDispatcher
{
    /// <summary>
    /// Signals that a batch task was enqueued.
    /// </summary>
    event EventHandler TaskEnqueued;

    /// <summary>
    /// Defines if the batch dispatcher shall dispatch tasks directly when they are enqueued.
    /// </summary>
    bool DispatchOnSchedule { get; set; }

    /// <summary>
    /// Begins dispatching batched tasks.
    /// </summary>
    void BeginDispatch(CancellationToken cancellationToken = default);
}
