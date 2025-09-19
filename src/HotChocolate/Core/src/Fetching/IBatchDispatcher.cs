using GreenDonut;

namespace HotChocolate.Fetching;

/// <summary>
/// <para>
/// A batch dispatcher is a component that coordinates efficient batch dispatching
/// between DataLoader and the Hot Chocolate execution engine.
/// </para>
/// <para>
/// This interface extends <see cref="IBatchScheduler"/> to allow DataLoader to
/// enqueue open batches that the dispatcher can dispatch in coordination with
/// the Hot Chocolate execution engine.
/// </para>
/// </summary>
public interface IBatchDispatcher
    : IBatchScheduler
    , IObservable<BatchDispatchEventArgs>
    , IDisposable
{
    /// <summary>
    /// Signals the dispatcher to begin processing queued batches. This method can be
    /// called to trigger immediate evaluation of pending batches, allowing the dispatcher
    /// to make intelligent decisions about when to dispatch based on current load and
    /// batch accumulation.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the dispatch operation.
    /// </param>
    void BeginDispatch(CancellationToken cancellationToken = default);
}
