using GreenDonut;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Processing;

#pragma warning disable CS0067
internal sealed class NoopBatchDispatcher : IBatchDispatcher
{
    public void Schedule(Batch batch)
    {
    }

    public IDisposable Subscribe(IObserver<BatchDispatchEventArgs> observer)
        => Disposable.Empty;

    public void BeginDispatch(CancellationToken cancellationToken = default)
    {
    }

    public void Dispose()
    {
    }

    public static NoopBatchDispatcher Instance { get; } = new();

    private class Disposable : IDisposable
    {
        public static IDisposable Empty { get; } = new Disposable();

        public void Dispose()
        {
        }
    }
}
