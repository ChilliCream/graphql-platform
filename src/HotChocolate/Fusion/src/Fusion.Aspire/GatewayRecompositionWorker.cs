using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Schedules schema recomposition runs for a single gateway. Triggers that arrive while a run
/// is already pending are coalesced into that run, and two runs for the same gateway never
/// overlap.
/// </summary>
internal sealed class GatewayRecompositionWorker
{
    private readonly Channel<bool> _triggers = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });
    private readonly string _gatewayName;
    private readonly Func<CancellationToken, Task> _recomposeAsync;
    private readonly TimeSpan _debounceDelay;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;

    public GatewayRecompositionWorker(
        string gatewayName,
        Func<CancellationToken, Task> recomposeAsync,
        TimeSpan debounceDelay,
        TimeProvider timeProvider,
        ILogger logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(gatewayName);
        ArgumentNullException.ThrowIfNull(recomposeAsync);
        ArgumentOutOfRangeException.ThrowIfLessThan(debounceDelay, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _gatewayName = gatewayName;
        _recomposeAsync = recomposeAsync;
        _debounceDelay = debounceDelay;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Requests a schema recomposition for the gateway. When a recomposition is already
    /// pending, the request is absorbed by the pending run.
    /// </summary>
    public void TriggerRecomposition() => _triggers.Writer.TryWrite(true);

    /// <summary>
    /// Processes recomposition requests until the token signals shutdown. A failing
    /// recomposition is logged and does not terminate the worker.
    /// </summary>
    public async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await _triggers.Reader.WaitToReadAsync(stoppingToken))
            {
                _triggers.Reader.TryRead(out _);

                if (_debounceDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_debounceDelay, _timeProvider, stoppingToken);
                }

                // Requests that arrived while waiting are covered by the run that starts now.
                _triggers.Reader.TryRead(out _);

                try
                {
                    await _recomposeAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Schema recomposition failed for {ResourceName}: {Error}",
                        _gatewayName,
                        exception.Message);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The application is shutting down.
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The schema recomposition worker for {ResourceName} stopped unexpectedly.",
                _gatewayName);
        }
    }
}
