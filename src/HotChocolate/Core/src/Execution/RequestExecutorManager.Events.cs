namespace HotChocolate.Execution;

internal sealed partial class RequestExecutorManager
{
    public IDisposable Subscribe(IObserver<RequestExecutorEvent> observer)
        => _events.Subscribe(observer);
}
