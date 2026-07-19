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
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

    private readonly ServiceBusReceiver? _receiver;
    private readonly Func<CancellationToken, Task> _peek;
    private readonly TimeSpan _interval;
    private readonly ILogger _logger;
    private readonly string _entityPath;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _runningTask;

    private QueueHeartbeat(
        ServiceBusReceiver? receiver,
        Func<CancellationToken, Task> peek,
        TimeSpan interval,
        ILogger logger,
        string entityPath)
    {
        _receiver = receiver;
        _peek = peek;
        _interval = interval;
        _logger = logger;
        _entityPath = entityPath;
        _runningTask = RunAsync(_cts.Token);
    }

    public QueueHeartbeat(
        ServiceBusReceiver receiver,
        TimeSpan interval,
        ILogger logger,
        string entityPath)
        : this(
            receiver,
            ct => receiver.PeekMessageAsync(cancellationToken: ct),
            interval,
            logger,
            entityPath)
    {
    }

    public QueueHeartbeat(
        ServiceBusReceiver receiver,
        ILogger logger,
        string entityPath)
        : this(receiver, DefaultInterval, logger, entityPath)
    {
    }

    internal QueueHeartbeat(
        Func<CancellationToken, Task> peek,
        TimeSpan interval,
        ILogger logger,
        string entityPath)
        : this(null, peek, interval, logger, entityPath)
    {
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            try
            {
                await _peek(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.ReplyQueueKeepAliveFailed(ex, _entityPath);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();

        try
        {
            await _runningTask.WaitAsync(StopTimeout);
        }
        catch (TimeoutException)
        {
        }
        catch (OperationCanceledException)
        {
        }

        _cts.Dispose();

        if (_receiver is not null)
        {
            await _receiver.DisposeAsync();
        }
    }
}
