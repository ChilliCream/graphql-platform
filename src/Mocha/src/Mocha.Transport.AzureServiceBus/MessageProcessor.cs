using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Wraps either a <see cref="ServiceBusProcessor"/> or a <see cref="ServiceBusSessionProcessor"/>
/// behind a uniform async lifecycle surface so the receive endpoint does not need to branch on
/// processor type for start, stop, and dispose operations.
/// </summary>
internal sealed class MessageProcessor : IAsyncDisposable
{
    private readonly Func<CancellationToken, Task> _start;
    private readonly Func<CancellationToken, Task> _stop;
    private readonly Func<ValueTask> _dispose;

    private MessageProcessor(
        Func<CancellationToken, Task> start,
        Func<CancellationToken, Task> stop,
        Func<ValueTask> dispose)
    {
        _start = start;
        _stop = stop;
        _dispose = dispose;
    }

    public static MessageProcessor ForProcessor(ServiceBusProcessor processor)
        => new(
            processor.StartProcessingAsync,
            processor.StopProcessingAsync,
            processor.DisposeAsync);

    public static MessageProcessor ForSessionProcessor(ServiceBusSessionProcessor processor)
        => new(
            processor.StartProcessingAsync,
            processor.StopProcessingAsync,
            processor.DisposeAsync);

    public Task StartProcessingAsync(CancellationToken cancellationToken)
        => _start(cancellationToken);

    public Task StopProcessingAsync(CancellationToken cancellationToken = default)
        => _stop(cancellationToken);

    public ValueTask DisposeAsync()
        => _dispose();
}
