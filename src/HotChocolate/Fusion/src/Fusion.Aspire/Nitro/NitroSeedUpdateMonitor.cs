using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal sealed class NitroSeedUpdateMonitor
{
    private static readonly TimeSpan s_initialReconnectDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_maxReconnectDelay = TimeSpan.FromMinutes(5);

    private readonly Channel<NitroStageSnapshot> _updates = Channel.CreateBounded<NitroStageSnapshot>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true
        });
    private readonly HashSet<string> _reportedFailures = [with(StringComparer.Ordinal)];
    private readonly Lock _failureSync = new();
    private readonly string _gatewayName;
    private readonly string _apiId;
    private readonly NitroSeedCoordinator _coordinator;
    private readonly SemaphoreSlim _compositionGate;
    private readonly Func<NitroSeedAdoption, CancellationToken, Task<bool>> _recomposeAsync;
    private readonly ILogger _resourceLogger;
    private readonly Action<NitroSeedCandidate> _notifyStaged;
    private readonly Action<NitroSeedAdoption> _reportAdoption;
    private readonly ResiliencePipeline _reconnectPipeline;
    private readonly ILogger _logger;
    private Task? _completion;
    private bool _degraded;
    private string? _lastProcessedIdentity;

    public NitroSeedUpdateMonitor(
        string gatewayName,
        string apiId,
        NitroSeedCoordinator coordinator,
        SemaphoreSlim compositionGate,
        Func<NitroSeedAdoption, CancellationToken, Task<bool>> recomposeAsync,
        ILogger resourceLogger,
        Action<NitroSeedCandidate> notifyStaged,
        Action<NitroSeedAdoption> reportAdoption,
        TimeProvider timeProvider,
        ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(compositionGate);
        ArgumentNullException.ThrowIfNull(recomposeAsync);
        ArgumentNullException.ThrowIfNull(resourceLogger);
        ArgumentNullException.ThrowIfNull(notifyStaged);
        ArgumentNullException.ThrowIfNull(reportAdoption);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _gatewayName = gatewayName;
        _apiId = apiId;
        _coordinator = coordinator;
        _compositionGate = compositionGate;
        _recomposeAsync = recomposeAsync;
        _resourceLogger = resourceLogger;
        _notifyStaged = notifyStaged;
        _reportAdoption = reportAdoption;
        _logger = logger;

        var pipelineBuilder = new ResiliencePipelineBuilder
        {
            TimeProvider = timeProvider
        };
        _reconnectPipeline = pipelineBuilder
            .AddRetry(
                new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<NitroStageReconnectException>(),
                    MaxRetryAttempts = int.MaxValue,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = s_initialReconnectDelay,
                    UseJitter = true,
                    MaxDelay = s_maxReconnectDelay,
                    DelayGenerator = arguments => new ValueTask<TimeSpan?>(
                        CreateReconnectDelay(arguments.AttemptNumber)),
                    OnRetry = arguments =>
                    {
                        RetryScheduled?.Invoke(arguments.RetryDelay);
                        return default;
                    }
                })
            .Build();
    }

    internal Task Completion => _completion ?? Task.CompletedTask;

    internal Action<TimeSpan>? RetryScheduled { get; set; }

    public void Start(CancellationToken stoppingToken)
    {
        if (_completion is null)
        {
            _completion = RunAsync(stoppingToken);
        }
    }

    public async Task SetAutoUpdateAsync(
        bool enabled,
        CancellationToken cancellationToken)
    {
        await _compositionGate.WaitAsync(cancellationToken);
        try
        {
            _coordinator.SetAutoUpdate(_gatewayName, enabled);
            _resourceLogger.LogInformation(
                "Automatic Nitro Fusion configuration updates were {AutoUpdateState} for "
                + "{ResourceName}.",
                enabled ? "enabled" : "disabled",
                _gatewayName);

            if (!enabled || _coordinator.TryAdoptStaged(_gatewayName) is not { } adoption)
            {
                return;
            }

            bool success;
            try
            {
                success = await _recomposeAsync(adoption, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _coordinator.RollBackAdoption(
                    _gatewayName,
                    adoption,
                    restoreStaged: true);
                _lastProcessedIdentity = null;
                throw;
            }
            catch (Exception exception)
            {
                _coordinator.RollBackAdoption(
                    _gatewayName,
                    adoption,
                    restoreStaged: true);
                _lastProcessedIdentity = null;
                _resourceLogger.LogWarning(
                    exception,
                    "The staged Nitro Fusion configuration for {ResourceName} could not be "
                    + "applied. The previous configuration remains active.",
                    _gatewayName);
                return;
            }

            if (!success)
            {
                _coordinator.RollBackAdoption(
                    _gatewayName,
                    adoption,
                    restoreStaged: true);
                _lastProcessedIdentity = null;
                return;
            }

            _coordinator.CompleteAdoption(_gatewayName, adoption);
            _reportAdoption(adoption);
        }
        finally
        {
            _compositionGate.Release();
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            var producer = ProduceUpdatesAsync(stoppingToken);
            var consumer = ConsumeUpdatesAsync(stoppingToken);
            await Task.WhenAll(producer, consumer);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "The Nitro stage update monitor for {ResourceName} stopped unexpectedly.",
                _gatewayName);
        }
    }

    private async Task ProduceUpdatesAsync(CancellationToken stoppingToken)
    {
        var delayBeforeFirstAttempt = false;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var firstAttempt = true;
                await _reconnectPipeline.ExecuteAsync(
                    async cancellationToken =>
                    {
                        if (firstAttempt && delayBeforeFirstAttempt)
                        {
                            firstAttempt = false;
                            throw new NitroStageReconnectException();
                        }

                        firstAttempt = false;
                        if (!await RunConnectionCycleAsync(cancellationToken))
                        {
                            throw new NitroStageReconnectException();
                        }
                    },
                    stoppingToken);

                // A connected SSE stream ended. Backend-initiated disconnects are normal, but
                // the next connection still waits for the initial reconnect delay. Starting a
                // new pipeline invocation resets the exponential failure count.
                delayBeforeFirstAttempt = true;
            }
        }
        finally
        {
            _updates.Writer.TryComplete();
        }
    }

    private async Task<bool> RunConnectionCycleAsync(CancellationToken cancellationToken)
    {
        NitroStageSubscription? subscription = null;
        try
        {
            subscription = await _coordinator.SubscribeToStageAsync(
                _apiId,
                _logger,
                cancellationToken);
            _degraded = false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (!_degraded)
            {
                _degraded = true;
                _resourceLogger.LogWarning(
                    "The Nitro stage subscription for {ResourceName} is unavailable. Current "
                    + "stage versions will be checked during reconnect attempts.",
                    _gatewayName);
            }

            _logger.LogDebug(
                exception,
                "The Nitro stage subscription for {ResourceName} could not be established.",
                _gatewayName);
        }

        NitroStageSnapshot? snapshot = null;
        try
        {
            // The subscription is established before this query. Events remain buffered until
            // enumeration starts, which closes the query-to-subscription publication race.
            snapshot = await _coordinator.GetLatestStageSnapshotAsync(
                _apiId,
                _logger,
                cancellationToken);
            if (snapshot is not null)
            {
                _updates.Writer.TryWrite(snapshot);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            ReportFailureOnce("query:" + NormalizeFailure(exception), exception);
        }

        if (subscription is null)
        {
            return false;
        }

        snapshot ??= new NitroStageSnapshot(
            fusionConfigurationId: null,
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal));

        await using (subscription)
        {
            try
            {
                await foreach (var change in subscription.ReadChangesAsync(cancellationToken))
                {
                    snapshot = snapshot.Apply(change);
                    _updates.Writer.TryWrite(snapshot);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    exception,
                    "The Nitro stage subscription for {ResourceName} was interrupted.",
                    _gatewayName);
            }
        }

        return true;
    }

    private async Task ConsumeUpdatesAsync(CancellationToken stoppingToken)
    {
        while (await _updates.Reader.WaitToReadAsync(stoppingToken))
        {
            // Waiting for the composition gate before reading leaves the channel's one slot
            // available to coalesce every stage change that arrives during a composition.
            await _compositionGate.WaitAsync(stoppingToken);
            try
            {
                if (!_updates.Reader.TryRead(out var update)
                    || string.Equals(
                        update.Identity,
                        _lastProcessedIdentity,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (await ProcessUpdateAsync(update, stoppingToken))
                {
                    _lastProcessedIdentity = update.Identity;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                ReportFailureOnce("process:" + NormalizeFailure(exception), exception);
            }
            finally
            {
                _compositionGate.Release();
            }
        }
    }

    private async Task<bool> ProcessUpdateAsync(
        NitroStageSnapshot update,
        CancellationToken cancellationToken)
    {
        var refresh = await _coordinator.DownloadFreshSeedAsync(
            _gatewayName,
            _apiId,
            update.Identity,
            _logger,
            suppressProviderLogs: true,
            cancellationToken);

        if (refresh.Candidate is not { } candidate)
        {
            ReportFailureOnce(
                "download:" + NormalizeFailure(refresh.FailureMessage),
                exception: null);
            return false;
        }

        if (_coordinator.GetSeedSnapshot(_gatewayName) is not { } current)
        {
            _coordinator.DeleteCandidate(candidate);
            return false;
        }

        if (string.Equals(
            current.Seed.SchemaHash,
            candidate.Seed.SchemaHash,
            StringComparison.Ordinal))
        {
            _coordinator.DeleteCandidate(candidate);
            if (_coordinator.DiscardStagedCandidate(_gatewayName) is { } staged)
            {
                _coordinator.DeleteCandidate(staged);
            }

            return true;
        }

        if (!_coordinator.IsAutoUpdateEnabled(_gatewayName))
        {
            _coordinator.StageCandidate(_gatewayName, candidate);
            _notifyStaged(candidate);
            return true;
        }

        if (_coordinator.TryAdoptCandidate(_gatewayName, candidate) is not { } adoption)
        {
            return false;
        }

        bool success;
        try
        {
            success = await _recomposeAsync(adoption, cancellationToken);
        }
        catch
        {
            _coordinator.RollBackAdoption(
                _gatewayName,
                adoption,
                restoreStaged: false);
            throw;
        }

        if (!success)
        {
            _coordinator.RollBackAdoption(
                _gatewayName,
                adoption,
                restoreStaged: false);
            return false;
        }

        _coordinator.CompleteAdoption(_gatewayName, adoption);
        _reportAdoption(adoption);
        return true;
    }

    private void ReportFailureOnce(string reason, Exception? exception)
    {
        lock (_failureSync)
        {
            if (_reportedFailures.Count >= 20 || !_reportedFailures.Add(reason))
            {
                return;
            }
        }

        _logger.LogWarning(
            exception,
            "Nitro stage update processing for {ResourceName} is unavailable: {Reason}",
            _gatewayName,
            reason);
    }

    private static string NormalizeFailure(Exception exception)
        => NormalizeFailure(exception.Message);

    private static string NormalizeFailure(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "unknown";
        }

        var firstLine = message.Split(['\r', '\n'], 2)[0];
        return firstLine.Length <= 160 ? firstLine : firstLine[..160];
    }

    private static TimeSpan CreateReconnectDelay(int attemptNumber)
    {
        var exponent = Math.Min(attemptNumber, 20);
        var exponentialMilliseconds = s_initialReconnectDelay.TotalMilliseconds
            * Math.Pow(2, exponent);
        var jitteredMilliseconds = exponentialMilliseconds
            * (1 + Random.Shared.NextDouble() * 0.25);

        return TimeSpan.FromMilliseconds(
            Math.Min(jitteredMilliseconds, s_maxReconnectDelay.TotalMilliseconds));
    }

    private sealed class NitroStageReconnectException : Exception;
}
