using System.Security.Cryptography;
using System.Threading.Channels;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal sealed class NitroSchemaValidationCoordinator
{
    private readonly Dictionary<string, GatewaySchemaValidationWorker> _workers = [with(StringComparer.Ordinal)];
    private readonly Lock _sync = new();
    private readonly NitroCompositionOptions _options;
    private readonly ResourceLoggerService _resourceLoggerService;
    private readonly INitroSchemaValidationNotifier? _notifier;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILoggerFactory _loggerFactory;

    public NitroSchemaValidationCoordinator(
        NitroCompositionOptions options,
        ResourceLoggerService resourceLoggerService,
        INitroSchemaValidationNotifier? notifier,
        IHostApplicationLifetime lifetime,
        ILoggerFactory loggerFactory)
    {
        _options = options;
        _resourceLoggerService = resourceLoggerService;
        _notifier = notifier;
        _lifetime = lifetime;
        _loggerFactory = loggerFactory;
    }

    public bool Schedule(IResource resource, byte[] schema)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(schema);

        try
        {
            return ScheduleCore(resource, schema);
        }
        catch (Exception exception)
        {
            _resourceLoggerService.GetLogger(resource).LogWarning(
                exception,
                "Nitro schema validation could not be scheduled for {ResourceName}. The installed "
                + "gateway schema is unaffected.",
                resource.Name);
            return false;
        }
    }

    internal NitroSchemaValidationReport? GetLatestReport(string resourceName)
    {
        lock (_sync)
        {
            return _workers.TryGetValue(resourceName, out var worker)
                ? worker.LatestReport
                : null;
        }
    }

    private bool ScheduleCore(IResource resource, byte[] schema)
    {
        var apiId = resource.GetNitroApiId();
        var coordinator = _options.Coordinator;
        if (apiId is null
            || coordinator is null
            || _notifier is null)
        {
            return false;
        }

        GatewaySchemaValidationWorker worker;
        lock (_sync)
        {
            if (!_workers.TryGetValue(resource.Name, out worker!))
            {
                var resourceLogger = _resourceLoggerService.GetLogger(resource);
                worker = new GatewaySchemaValidationWorker(
                    resource.Name,
                    coordinator.Stage,
                    (request, cancellationToken) => coordinator.ValidateSchemaAsync(
                        apiId,
                        request.Schema,
                        request.SchemaHash,
                        resourceLogger,
                        cancellationToken),
                    resourceLogger,
                    _notifier,
                    _lifetime.ApplicationStopping,
                    _loggerFactory.CreateLogger<GatewaySchemaValidationWorker>());
                _workers.Add(resource.Name, worker);
            }
        }

        var hash = Convert.ToHexString(SHA256.HashData(schema));
        return worker.Enqueue(new GatewaySchemaValidationRequest(schema, hash));
    }
}

internal sealed record GatewaySchemaValidationRequest(byte[] Schema, string SchemaHash);

internal sealed class GatewaySchemaValidationWorker
{
    private readonly Channel<ValidationGeneration> _requests =
        Channel.CreateBounded<ValidationGeneration>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            });
    private readonly Dictionary<string, NitroSchemaValidationReport> _terminalReports = [with(StringComparer.Ordinal)];
    private readonly HashSet<string> _reportedUnavailableReasons = [with(StringComparer.Ordinal)];
    private readonly Lock _sync = new();
    private readonly Lock _reportApplicationSync = new();
    private readonly string _gatewayName;
    private readonly string _stage;
    private readonly Func<GatewaySchemaValidationRequest, CancellationToken, Task<NitroSchemaValidationReport>> _validateAsync;
    private readonly ILogger _resourceLogger;
    private readonly INitroSchemaValidationNotifier _notifier;
    private readonly CancellationToken _stoppingToken;
    private readonly ILogger _logger;
    private CancellationTokenSource? _activeCancellation;
    private readonly Task _completion;
    private NitroSchemaValidationReport? _latestReport;
    private string? _pendingHash;
    private long _generation;
    private bool _hasUnresolvedViolations;

    public GatewaySchemaValidationWorker(
        string gatewayName,
        string stage,
        Func<GatewaySchemaValidationRequest, CancellationToken, Task<NitroSchemaValidationReport>>
            validateAsync,
        ILogger resourceLogger,
        INitroSchemaValidationNotifier notifier,
        CancellationToken stoppingToken,
        ILogger logger)
    {
        _gatewayName = gatewayName;
        _stage = stage;
        _validateAsync = validateAsync;
        _resourceLogger = resourceLogger;
        _notifier = notifier;
        _stoppingToken = stoppingToken;
        _logger = logger;

        _completion = RunAsync();
    }

    public bool Enqueue(GatewaySchemaValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        NitroSchemaValidationReport? cached;
        long nextGeneration;
        var queued = false;

        lock (_sync)
        {
            if (string.Equals(_pendingHash, request.SchemaHash, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                _activeCancellation?.Cancel();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "The superseded Nitro schema validation for {ResourceName} could not be "
                    + "canceled cleanly.",
                    _gatewayName);
            }

            nextGeneration = ++_generation;

            if (_terminalReports.TryGetValue(request.SchemaHash, out cached))
            {
                _pendingHash = null;
            }
            else
            {
                _pendingHash = request.SchemaHash;
                queued = _requests.Writer.TryWrite(
                    new ValidationGeneration(nextGeneration, request));
                if (!queued)
                {
                    _pendingHash = null;
                }
            }
        }

        if (cached is not null)
        {
            ApplyReport(cached, nextGeneration);
            return false;
        }

        return queued;
    }

    internal NitroSchemaValidationReport? LatestReport
    {
        get
        {
            lock (_sync)
            {
                return _latestReport;
            }
        }
    }

    internal Task Completion => _completion;

    private async Task RunAsync()
    {
        try
        {
            await foreach (var request in _requests.Reader.ReadAllAsync(_stoppingToken))
            {
                using var activeCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken);

                lock (_sync)
                {
                    if (request.Generation != _generation)
                    {
                        continue;
                    }

                    _activeCancellation = activeCancellation;
                }

                NitroSchemaValidationReport report;
                try
                {
                    report = await _validateAsync(request.Request, activeCancellation.Token);
                }
                catch (OperationCanceledException) when (activeCancellation.IsCancellationRequested)
                {
                    continue;
                }
                catch (Exception exception)
                {
                    report = NitroSchemaValidationReport.Unavailable(
                        request.Request.SchemaHash,
                        $"Nitro schema validation was unavailable: {exception.Message}",
                        DateTimeOffset.UtcNow);
                }
                finally
                {
                    lock (_sync)
                    {
                        if (ReferenceEquals(_activeCancellation, activeCancellation))
                        {
                            _activeCancellation = null;
                        }
                    }
                }

                lock (_sync)
                {
                    if (request.Generation != _generation)
                    {
                        continue;
                    }

                    _pendingHash = null;
                    _terminalReports[request.Request.SchemaHash] = report;
                }

                ApplyReport(report, request.Generation);
            }
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The Nitro schema validation worker for {ResourceName} stopped unexpectedly.",
                _gatewayName);
        }
    }

    private void ApplyReport(NitroSchemaValidationReport report, long generation)
    {
        lock (_reportApplicationSync)
        {
            ApplyReportSynchronized(report, generation);
        }
    }

    private void ApplyReportSynchronized(
        NitroSchemaValidationReport report,
        long generation)
    {
        var transition = NotificationTransition.None;
        var shouldLogUnavailable = false;
        string? unavailableLogReason = null;

        lock (_sync)
        {
            var previousReport = _latestReport;
            if (generation != _generation)
            {
                return;
            }

            _latestReport = report;

            switch (report.Status)
            {
                case NitroSchemaValidationStatus.Violations:
                    if (previousReport?.Status is not NitroSchemaValidationStatus.Violations
                        || !string.Equals(
                            previousReport.Fingerprint,
                            report.Fingerprint,
                            StringComparison.Ordinal))
                    {
                        transition = NotificationTransition.Violations;
                    }

                    _hasUnresolvedViolations = true;
                    break;

                case NitroSchemaValidationStatus.Passed:
                    if (_hasUnresolvedViolations)
                    {
                        transition = NotificationTransition.Restored;
                    }

                    _hasUnresolvedViolations = false;
                    break;

                case NitroSchemaValidationStatus.Unavailable:
                    var reason = NormalizeUnavailableReason(report.UnavailableReason);
                    unavailableLogReason = reason;
                    shouldLogUnavailable =
                        _reportedUnavailableReasons.Count < 20
                        && _reportedUnavailableReasons.Add(reason);
                    break;
            }
        }

        switch (report.Status)
        {
            case NitroSchemaValidationStatus.Violations:
                _resourceLogger.LogError("{Findings}", NitroSchemaValidationFormatter.Format(report));
                break;

            case NitroSchemaValidationStatus.Passed when transition is NotificationTransition.Restored:
                _resourceLogger.LogInformation(
                    "Nitro client-contract validation passed again for {ResourceName}.",
                    _gatewayName);
                break;

            case NitroSchemaValidationStatus.Unavailable when shouldLogUnavailable:
                _resourceLogger.LogWarning(
                    "Nitro schema validation is unavailable for {ResourceName}: {Reason}",
                    _gatewayName,
                    unavailableLogReason);
                break;
        }

        if (transition is not NotificationTransition.None)
        {
            Notify(transition, report);
        }
    }

    private void Notify(
        NotificationTransition transition,
        NitroSchemaValidationReport report)
    {
        var message = transition is NotificationTransition.Restored
            ? $"Client contracts restored (gateway '{_gatewayName}', stage '{_stage}')."
            : $"Client contracts broken: {report.ClientCount} clients, "
                + $"{report.OperationCount} operations affected (gateway '{_gatewayName}', "
                + $"stage '{_stage}').";

        if (transition is NotificationTransition.Restored)
        {
            _notifier.NotifyRestored(message);
        }
        else
        {
            _notifier.NotifyViolations(message);
        }
    }

    private static string NormalizeUnavailableReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return "unknown";
        }

        var separator = reason.IndexOf(':');
        return separator < 0
            ? Truncate(reason, 128)
            : Truncate(reason[..separator], 128);
    }

    private static string Truncate(string value, int length)
        => value.Length <= length ? value : value[..length];

    private sealed record ValidationGeneration(
        long Generation,
        GatewaySchemaValidationRequest Request);

    private enum NotificationTransition
    {
        None,
        Violations,
        Restored
    }
}
