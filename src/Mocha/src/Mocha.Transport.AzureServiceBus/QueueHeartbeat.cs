using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Periodically peeks at a queue to reset its <c>AutoDeleteOnIdle</c> timer, keeping
/// framework-managed reply queues alive even when no replies have flowed for the idle window.
/// </summary>
internal sealed class QueueHeartbeat : IAsyncDisposable
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(12);

    private readonly ServiceBusReceiver _receiver;
    private readonly ILogger _logger;
    private readonly string _entityPath;
    private readonly Timer _timer;

    public QueueHeartbeat(
        ServiceBusReceiver receiver,
        TimeSpan interval,
        ILogger logger,
        string entityPath)
    {
        _receiver = receiver;
        _logger = logger;
        _entityPath = entityPath;
        _timer = new Timer(OnTimerFired, null, interval, interval);
    }

    public QueueHeartbeat(
        ServiceBusReceiver receiver,
        ILogger logger,
        string entityPath)
        : this(receiver, DefaultInterval, logger, entityPath)
    {
    }

    private void OnTimerFired(object? state) => _ = KeepAliveAsync();

    private async Task KeepAliveAsync()
    {
        try
        {
            // Peek is documented activity that resets AutoDeleteOnIdle, keeping the
            // framework-managed reply queue alive even when no replies have flowed
            // for the idle window.
            await _receiver.PeekMessageAsync();
        }
        catch (Exception ex)
        {
            _logger.ReplyQueueKeepAliveFailed(ex, _entityPath);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _timer.DisposeAsync();
        await _receiver.DisposeAsync();
    }
}
