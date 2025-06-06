namespace HotChocolate.Execution;

internal sealed partial class RequestExecutorProvider
{
    public IDisposable Subscribe(IObserver<RequestExecutorEvent> observer)
        => _events.Subscribe(observer);
}
