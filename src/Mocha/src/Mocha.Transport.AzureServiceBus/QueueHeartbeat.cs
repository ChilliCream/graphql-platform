using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Periodically peeks at a queue to reset its <c>AutoDeleteOnIdle</c> timer, keeping a queue
/// with an idle deletion window alive while its receive endpoint is active.
/// </summary>
internal sealed class QueueHeartbeat : IAsyncDisposable
{
    private static readonly TimeSpan s_stopTimeout = TimeSpan.FromSeconds(5);

    private readonly ServiceBusReceiver? _receiver;
    private readonly Func<CancellationToken, Task> _peek;
    private readonly TimeSpan _interval;
    private readonly ILogger _logger;
    private readonly string _entityPath;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _runningTask;
    private bool _disposed;

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

    public QueueHeartbeat(ServiceBusReceiver receiver, TimeSpan autoDeleteOnIdle, ILogger logger, string entityPath)
        : this(
            receiver,
            ct => receiver.PeekMessageAsync(cancellationToken: ct),
            autoDeleteOnIdle >= AzureServiceBusReceiveEndpointConfiguration.TemporaryDefaults.MinimumAutoDeleteOnIdle
                ? autoDeleteOnIdle / 2
                : throw new ArgumentOutOfRangeException(
                    nameof(autoDeleteOnIdle),
                    autoDeleteOnIdle,
                    "AutoDeleteOnIdle must be at least "
                        + $"{AzureServiceBusReceiveEndpointConfiguration.TemporaryDefaults.MinimumAutoDeleteOnIdle}."),
            logger,
            entityPath)
    { }

    internal QueueHeartbeat(Func<CancellationToken, Task> peek, TimeSpan interval, ILogger logger, string entityPath)
        : this(null, peek, interval, logger, entityPath) { }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, ct).ConfigureAwait(false);
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
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _cts.CancelAsync();

        try
        {
            await _runningTask.WaitAsync(s_stopTimeout);
        }
        catch (TimeoutException) { }
        catch (OperationCanceledException) { }

        _cts.Dispose();

        if (_receiver is not null)
        {
            await _receiver.DisposeAsync();
        }
    }
}
