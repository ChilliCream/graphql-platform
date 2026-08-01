using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal sealed class NitroSeedUpdateService
{
    private readonly Dictionary<string, NitroSeedUpdateMonitor> _monitors = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, NitroSeedCoordinator> _coordinators = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, IResource> _resources = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, HashSet<string>> _notifiedVersions = [with(StringComparer.Ordinal)];
    private readonly HashSet<string> _notifiedAdoptedHashes = [with(StringComparer.Ordinal)];
    private readonly Lock _sync = new();
    private readonly ResourceLoggerService _resourceLoggerService;
    private readonly INitroSeedUpdateNotifier _notifier;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TimeProvider _timeProvider;

    public NitroSeedUpdateService(
        ResourceLoggerService resourceLoggerService,
        INitroSeedUpdateNotifier notifier,
        IHostApplicationLifetime lifetime,
        ILoggerFactory loggerFactory,
        TimeProvider timeProvider)
    {
        _resourceLoggerService = resourceLoggerService;
        _notifier = notifier;
        _lifetime = lifetime;
        _loggerFactory = loggerFactory;
        _timeProvider = timeProvider;
    }

    internal int MonitorCount
    {
        get
        {
            lock (_sync)
            {
                return _monitors.Count;
            }
        }
    }

    public bool IsAutoUpdateEnabled(string gatewayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);

        lock (_sync)
        {
            return _coordinators.TryGetValue(gatewayName, out var coordinator)
                && coordinator.IsAutoUpdateEnabled(gatewayName);
        }
    }

    public bool IsReady(string gatewayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);

        lock (_sync)
        {
            return _monitors.ContainsKey(gatewayName);
        }
    }

    public void Start(
        IResource gateway,
        string apiId,
        NitroStageResource stage,
        NitroSeedCoordinator coordinator,
        SemaphoreSlim compositionGate,
        Func<NitroSeedAdoption, CancellationToken, Task<bool>> recomposeAsync)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(compositionGate);
        ArgumentNullException.ThrowIfNull(recomposeAsync);

        if (!stage.Api.Nitro.SeedUpdates.Enabled)
        {
            return;
        }

        NitroSeedUpdateMonitor monitor;
        lock (_sync)
        {
            if (_monitors.ContainsKey(gateway.Name))
            {
                return;
            }

            monitor = new NitroSeedUpdateMonitor(
                gateway.Name,
                apiId,
                coordinator,
                compositionGate,
                recomposeAsync,
                _resourceLoggerService.GetLogger(gateway),
                candidate => NotifyStaged(gateway.Name, coordinator.Stage, candidate),
                adoption => ReportAdoption(gateway.Name, coordinator.Stage, adoption),
                _timeProvider,
                _loggerFactory.CreateLogger<NitroSeedUpdateMonitor>());
            _monitors.Add(gateway.Name, monitor);
            _coordinators.Add(gateway.Name, coordinator);
            _resources[gateway.Name] = gateway;
        }

        // Starting outside the service lock prevents a synchronously completing test transport,
        // or any future in-memory transport, from re-entering the reporting callbacks under it.
        monitor.Start(_lifetime.ApplicationStopping);
    }

    public async Task<ExecuteCommandResult> SetAutoUpdateAsync(
        string gatewayName,
        bool enabled,
        CancellationToken cancellationToken)
    {
        NitroSeedUpdateMonitor? monitor;
        lock (_sync)
        {
            _monitors.TryGetValue(gatewayName, out monitor);
        }

        if (monitor is null)
        {
            return CommandResults.Failure("Nitro stage update monitoring is not ready.");
        }

        await monitor.SetAutoUpdateAsync(enabled, cancellationToken);

        return CommandResults.Success(
            enabled ? "Automatic Nitro updates enabled" : "Automatic Nitro updates disabled");
    }

    public void ReportAdoption(
        string gatewayName,
        string stage,
        NitroSeedAdoption adoption)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentNullException.ThrowIfNull(adoption);

        IResource? resource;
        lock (_sync)
        {
            _resources.TryGetValue(gatewayName, out resource);
        }

        if (resource is not null)
        {
            _resourceLoggerService.GetLogger(resource).LogInformation(
                "Adopted a newer Fusion configuration for {ResourceName}: schema hash "
                + "{PreviousHash} to {NewHash}, downloaded at {DownloadedAt}.",
                gatewayName,
                Prefix(adoption.Previous.Seed.SchemaHash),
                Prefix(adoption.Current.Seed.SchemaHash),
                adoption.Current.Seed.DownloadedAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + "Z");
        }

        var shouldNotify = false;
        lock (_sync)
        {
            var adoptedHashKey = gatewayName + "\n" + adoption.Current.Seed.SchemaHash;
            if (_notifiedAdoptedHashes.Add(adoptedHashKey)
                && MarkVersionNotified(gatewayName, adoption.Candidate.VersionIdentity))
            {
                shouldNotify = true;
            }
        }

        if (shouldNotify)
        {
            _notifier.NotifyAdopted(
                $"Recomposed '{gatewayName}' against a newer Fusion configuration "
                + $"(stage '{stage}').");
        }
    }

    private void NotifyStaged(
        string gatewayName,
        string stage,
        NitroSeedCandidate candidate)
    {
        var shouldNotify = false;
        lock (_sync)
        {
            shouldNotify = MarkVersionNotified(gatewayName, candidate.VersionIdentity);
        }

        if (shouldNotify)
        {
            _notifier.NotifyStaged(
                $"A newer Fusion configuration for '{gatewayName}' (stage '{stage}') was "
                + "downloaded and staged. It is applied on the next recomposition.");
        }
    }

    private bool MarkVersionNotified(string gatewayName, string versionIdentity)
    {
        if (!_notifiedVersions.TryGetValue(gatewayName, out var identities))
        {
            identities = new HashSet<string>(StringComparer.Ordinal);
            _notifiedVersions.Add(gatewayName, identities);
        }

        return identities.Add(versionIdentity);
    }

    private static string Prefix(string hash)
        => hash.Length <= 12 ? hash : hash[..12];
}
