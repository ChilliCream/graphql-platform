using GreenDonut;

namespace HotChocolate.Fetching;

/// <summary>
/// The execution engine batch dispatcher.
/// </summary>
public interface IBatchDispatcher
    : IBatchScheduler
    , IObservable<BatchDispatchEventArgs>
    , IDisposable
{
    void BeginDispatch(CancellationToken cancellationToken = default);
}

public readonly record struct BatchDispatchEventArgs(BatchDispatchEventType Type);

public enum BatchDispatchEventType
{
    Enqueued,
    Evaluated,
    Dispatched
}
