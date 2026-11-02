using Mocha.Threading;

namespace Mocha.Outbox;

internal sealed class MessageBusOutboxSignal : IDisposable, IOutboxSignal
{
    private readonly AsyncAutoResetEvent _resetEvent = new(true);

    public void Set() => _resetEvent.Set();

    public Task WaitAsync(CancellationToken cancellationToken) => _resetEvent.WaitAsync(cancellationToken);

    public void Dispose()
    {
        _resetEvent.Dispose();
    }
}
