using System.Security.Cryptography;
using System.Text.Json;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Provides the fusion configuration that the gateways of a run compose against. Each gateway gets
/// its own private copy of the configuration that Nitro serves for the configured stage, and that
/// copy is the only base its compositions build on for the rest of the run.
/// </summary>
internal sealed class NitroSeedCoordinator
{
    private readonly Dictionary<string, GatewaySeedState> _statesByGateway = [with(StringComparer.Ordinal)];
    private readonly Lock _sync = new();
    private readonly NitroConnectionResolver _connectionResolver;
    private readonly NitroSeedProvider _seedProvider;
    private readonly INitroSchemaValidator _schemaValidator;
    private readonly INitroStageUpdateClient _stageUpdateClient;
    private readonly INitroCompositionSettingsClient _compositionSettingsClient;
    private readonly string _runSeedDirectory;
    private bool _initialAutoUpdate;
    private long _nextRunSeedId;

    /// <summary>
    /// Initializes a new instance of <see cref="NitroSeedCoordinator"/>.
    /// </summary>
    /// <param name="stage">
    /// The name of the stage whose fusion configuration the gateways compose against.
    /// </param>
    /// <param name="connectionResolver">
    /// The resolver for the connection to the Nitro API.
    /// </param>
    /// <param name="seedProvider">
    /// The provider of the fusion configurations.
    /// </param>
    /// <param name="schemaValidator">
    /// The client that validates composed gateway schemas against Nitro.
    /// </param>
    /// <param name="stageUpdateClient">
    /// The client that observes the current version of the Nitro stage.
    /// </param>
    /// <param name="compositionSettingsClient">
    /// The client that gets the composition settings of the Nitro stage.
    /// </param>
    /// <param name="runSeedDirectory">
    /// The directory that holds the private copies of this run.
    /// </param>
    /// <param name="initialAutoUpdate">
    /// Whether newly observed configurations are applied automatically by default.
    /// </param>
    public NitroSeedCoordinator(
        string stage,
        NitroConnectionResolver connectionResolver,
        NitroSeedProvider seedProvider,
        INitroSchemaValidator schemaValidator,
        INitroStageUpdateClient stageUpdateClient,
        INitroCompositionSettingsClient compositionSettingsClient,
        string runSeedDirectory,
        bool initialAutoUpdate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentNullException.ThrowIfNull(connectionResolver);
        ArgumentNullException.ThrowIfNull(seedProvider);
        ArgumentNullException.ThrowIfNull(schemaValidator);
        ArgumentNullException.ThrowIfNull(stageUpdateClient);
        ArgumentNullException.ThrowIfNull(compositionSettingsClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(runSeedDirectory);

        Stage = stage;
        _connectionResolver = connectionResolver;
        _seedProvider = seedProvider;
        _schemaValidator = schemaValidator;
        _stageUpdateClient = stageUpdateClient;
        _compositionSettingsClient = compositionSettingsClient;
        _runSeedDirectory = runSeedDirectory;
        _initialAutoUpdate = initialAutoUpdate;
    }

    /// <summary>
    /// Gets the name of the stage whose fusion configuration the gateways compose against.
    /// </summary>
    public string Stage { get; }

    /// <summary>
    /// Builds the Nitro access layer with its production configuration.
    /// </summary>
    /// <param name="stage">
    /// The name of the stage whose fusion configuration the gateways compose against.
    /// </param>
    /// <param name="initialAutoUpdate">
    /// Whether newly observed configurations are applied automatically by default.
    /// </param>
    public static NitroSeedCoordinator CreateProduction(
        string stage,
        bool initialAutoUpdate = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);

        var timeProvider = TimeProvider.System;
        var httpClient = new HttpClient();
        var connectionResolver = new NitroConnectionResolver(
            new NitroSessionManager(
                new NitroSessionReader(
                    NitroDefaults.GetSessionFilePath(),
                    NitroDefaults.SessionRereadDelay),
                new NitroTokenRefreshClient(httpClient),
                timeProvider,
                NitroDefaults.AccessTokenExpiryGrace),
            SystemNitroEnvironment.Instance,
            NitroDefaults.ApiUrl);
        var seedProvider = new NitroSeedProvider(
            new NitroFusionConfigurationDownloader(
                httpClient,
                NitroDefaults.DownloadRetryPolicy,
                timeProvider),
            new NitroSeedCache(NitroDefaults.GetSeedCacheDirectory(), timeProvider),
            new NitroApiLookupClient(
                GraphQLHttpClient.Create(httpClient, disposeHttpClient: false)));
        var schemaValidator = new NitroSchemaValidator(
            GraphQLHttpClient.Create(httpClient, disposeHttpClient: false),
            timeProvider,
            NullLogger<NitroSchemaValidator>.Instance);
        var stageUpdateClient = new NitroStageUpdateClient(
            GraphQLHttpClient.Create(httpClient, disposeHttpClient: false));
        var compositionSettingsClient = new NitroCompositionSettingsClient(
            GraphQLHttpClient.Create(httpClient, disposeHttpClient: false));

        return new NitroSeedCoordinator(
            stage,
            connectionResolver,
            seedProvider,
            schemaValidator,
            stageUpdateClient,
            compositionSettingsClient,
            NitroDefaults.CreateRunSeedDirectoryPath(),
            initialAutoUpdate);
    }

    public Task<NitroConnection> ResolveConnectionAsync(
        ILogger logger,
        CancellationToken cancellationToken)
        => _connectionResolver.ResolveAsync(logger, cancellationToken);

    public async Task<NitroSchemaValidationReport> ValidateSchemaAsync(
        string apiId,
        byte[] schema,
        string schemaHash,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var connection = await _connectionResolver.ResolveAsync(logger, cancellationToken);
        return await _schemaValidator.ValidateAsync(
            connection,
            apiId,
            Stage,
            schema,
            schemaHash,
            cancellationToken);
    }

    public async Task<NitroStageSubscription> SubscribeToStageAsync(
        string apiId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var connection = await _connectionResolver.ResolveAsync(logger, cancellationToken);
        return await _stageUpdateClient.SubscribeAsync(
            connection,
            apiId,
            Stage,
            cancellationToken);
    }

    public async Task<NitroStageSnapshot?> GetLatestStageSnapshotAsync(
        string apiId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var connection = await _connectionResolver.ResolveAsync(logger, cancellationToken);
        return await _stageUpdateClient.GetLatestSnapshotAsync(
            connection,
            apiId,
            Stage,
            cancellationToken);
    }

    /// <summary>
    /// Acquires the fusion configuration of a gateway and keeps it as the configuration of that
    /// gateway for the rest of the run.
    /// </summary>
    /// <param name="gatewayName">
    /// The name of the gateway resource.
    /// </param>
    /// <param name="apiId">
    /// The id of the Nitro api that carries the fusion configuration of the gateway.
    /// </param>
    /// <param name="logger">
    /// The logger of the gateway. It receives the resolved Nitro API URL, the credential source
    /// and the warning when the configuration is not fresh.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public async Task<NitroSeedAcquisition> AcquireSeedAsync(
        string gatewayName,
        string apiId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentNullException.ThrowIfNull(logger);

        var connection = await _connectionResolver.ResolveAsync(logger, cancellationToken);
        var settingsTask = connection.Credential.Kind is NitroCredentialKind.None
            ? Task.FromResult<CompositionSettings?>(null)
            : _compositionSettingsClient.GetAsync(
                connection,
                apiId,
                Stage,
                cancellationToken);
        var result = await _seedProvider.GetSeedAsync(
            connection,
            apiId,
            Stage,
            logger,
            cancellationToken);

        CompositionSettings? stageSettings;
        try
        {
            stageSettings = await settingsTask;
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException
            && result.Outcome is NitroSeedOutcome.ServedFromCache)
        {
            stageSettings = null;
            logger.LogWarning(
                exception,
                "The composition settings for the api {ApiId} and the stage {Stage} could not "
                + "be fetched from Nitro. The settings in the cached fusion configuration will "
                + "be used.",
                apiId,
                Stage);
        }

        if (result.Outcome is NitroSeedOutcome.Unavailable)
        {
            return NitroSeedAcquisition.Failed(result.Message!);
        }

        string filePath;
        DateTimeOffset downloadedAt;
        bool isFresh;

        if (result.Outcome is NitroSeedOutcome.NotFound)
        {
            filePath = await CreateInitialSeedAsync(
                gatewayName,
                stageSettings,
                cancellationToken);
            downloadedAt = DateTimeOffset.UtcNow;
            isFresh = true;

            logger.LogInformation(
                "The api {ApiId} has no fusion configuration for the stage {Stage} yet. "
                + "Composing from Nitro's initial composition settings.",
                apiId,
                Stage);
        }
        else
        {
            filePath = CopyToRunDirectory(gatewayName, result.FilePath!);
            await ApplyCompositionSettingsAsync(filePath, stageSettings, logger, cancellationToken);
            downloadedAt = result.DownloadedAt!.Value;
            isFresh = result.Outcome is NitroSeedOutcome.Downloaded;
        }

        var seed = new NitroGatewaySeed(
            apiId,
            Stage,
            filePath,
            downloadedAt,
            isFresh,
            await ComputeSchemaHashAsync(filePath, cancellationToken));

        NitroGatewaySeed? replacedSeed = null;
        NitroSeedCandidate? discardedStaged = null;
        lock (_sync)
        {
            if (_statesByGateway.TryGetValue(gatewayName, out var state))
            {
                replacedSeed = state.Current;
                discardedStaged = state.Staged;
                state.Current = seed;
                state.Generation++;
                state.Staged = null;
            }
            else
            {
                _statesByGateway.Add(
                    gatewayName,
                    new GatewaySeedState(seed, _initialAutoUpdate));
            }
        }

        TryDeleteUnlessCurrent(replacedSeed?.FilePath, seed.FilePath);
        TryDeleteUnlessCurrent(discardedStaged?.Seed.FilePath, seed.FilePath);

        return NitroSeedAcquisition.Acquired(seed);
    }

    /// <summary>
    /// Gets the fusion configuration that was acquired for a gateway in this run.
    /// </summary>
    /// <param name="gatewayName">
    /// The name of the gateway resource.
    /// </param>
    /// <returns>
    /// The fusion configuration, or <c>null</c> when none was acquired for the gateway, which is
    /// the case when the gateway failed to start.
    /// </returns>
    public NitroGatewaySeed? GetSeed(string gatewayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);

        lock (_sync)
        {
            return _statesByGateway.GetValueOrDefault(gatewayName)?.Current;
        }
    }

    public NitroSeedSnapshot? GetSeedSnapshot(string gatewayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);

        lock (_sync)
        {
            return _statesByGateway.TryGetValue(gatewayName, out var state)
                ? new NitroSeedSnapshot(state.Current, state.Generation)
                : null;
        }
    }

    public async Task<NitroSeedRefreshResult> DownloadFreshSeedAsync(
        string gatewayName,
        string apiId,
        string versionIdentity,
        ILogger logger,
        bool suppressProviderLogs,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentException.ThrowIfNullOrWhiteSpace(versionIdentity);
        ArgumentNullException.ThrowIfNull(logger);

        var connection = await _connectionResolver.ResolveAsync(logger, cancellationToken);
        var providerLogger = suppressProviderLogs ? NullLogger.Instance : logger;
        var settingsTask = _compositionSettingsClient.GetAsync(
            connection,
            apiId,
            Stage,
            cancellationToken);
        var result = await _seedProvider.GetSeedAsync(
            connection,
            apiId,
            Stage,
            providerLogger,
            cancellationToken);

        if (result.Outcome is not NitroSeedOutcome.Downloaded)
        {
            try
            {
                await settingsTask;
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException
                && result.Outcome is not NitroSeedOutcome.Downloaded)
            {
                // The archive result is already the reason this refresh cannot be used.
            }

            return NitroSeedRefreshResult.Failed(
                result.Message ?? "Nitro did not return a fresh Fusion configuration.");
        }

        var stageSettings = await settingsTask;
        var filePath = CopyToRunDirectory(gatewayName, result.FilePath!);
        await ApplyCompositionSettingsAsync(filePath, stageSettings, logger, cancellationToken);
        var seed = new NitroGatewaySeed(
            apiId,
            Stage,
            filePath,
            result.DownloadedAt!.Value,
            IsFresh: true,
            await ComputeSchemaHashAsync(filePath, cancellationToken));

        return NitroSeedRefreshResult.Downloaded(
            new NitroSeedCandidate(seed, versionIdentity));
    }

    public bool IsAutoUpdateEnabled(string gatewayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);

        lock (_sync)
        {
            return _statesByGateway.TryGetValue(gatewayName, out var state)
                ? state.AutoUpdate
                : _initialAutoUpdate;
        }
    }

    public void SetInitialAutoUpdate(bool enabled)
    {
        lock (_sync)
        {
            _initialAutoUpdate = enabled;

            foreach (var state in _statesByGateway.Values)
            {
                state.AutoUpdate = enabled;
            }
        }
    }

    public void SetAutoUpdate(string gatewayName, bool enabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);

        lock (_sync)
        {
            if (_statesByGateway.TryGetValue(gatewayName, out var state))
            {
                state.AutoUpdate = enabled;
            }
        }
    }

    public void StageCandidate(string gatewayName, NitroSeedCandidate candidate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);
        ArgumentNullException.ThrowIfNull(candidate);

        NitroSeedCandidate? replaced = null;
        lock (_sync)
        {
            if (_statesByGateway.TryGetValue(gatewayName, out var state))
            {
                replaced = state.Staged;
                state.Staged = candidate;
            }
        }

        TryDeleteUnlessCurrent(replaced?.Seed.FilePath, candidate.Seed.FilePath);
    }

    public NitroSeedAdoption? TryAdoptCandidate(
        string gatewayName,
        NitroSeedCandidate candidate,
        bool wasStaged = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);
        ArgumentNullException.ThrowIfNull(candidate);

        NitroSeedCandidate? discardedStaged;
        NitroSeedAdoption adoption;
        lock (_sync)
        {
            if (!_statesByGateway.TryGetValue(gatewayName, out var state))
            {
                return null;
            }

            discardedStaged = state.Staged;
            var previous = new NitroSeedSnapshot(state.Current, state.Generation);
            state.Current = candidate.Seed;
            state.Generation++;
            state.Staged = null;

            adoption = new NitroSeedAdoption(
                previous,
                new NitroSeedSnapshot(state.Current, state.Generation),
                candidate,
                wasStaged);
        }

        TryDeleteUnlessCurrent(discardedStaged?.Seed.FilePath, candidate.Seed.FilePath);
        return adoption;
    }

    public NitroSeedAdoption? TryAdoptStaged(string gatewayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);

        lock (_sync)
        {
            if (!_statesByGateway.TryGetValue(gatewayName, out var state)
                || state.Staged is not { } staged)
            {
                return null;
            }

            var previous = new NitroSeedSnapshot(state.Current, state.Generation);
            state.Current = staged.Seed;
            state.Generation++;
            state.Staged = null;

            return new NitroSeedAdoption(
                previous,
                new NitroSeedSnapshot(state.Current, state.Generation),
                staged,
                WasStaged: true);
        }
    }

    public void RollBackAdoption(
        string gatewayName,
        NitroSeedAdoption adoption,
        bool restoreStaged)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);
        ArgumentNullException.ThrowIfNull(adoption);

        var deleteCandidate = false;
        lock (_sync)
        {
            if (!_statesByGateway.TryGetValue(gatewayName, out var state)
                || state.Generation != adoption.Current.Generation)
            {
                return;
            }

            state.Current = adoption.Previous.Seed;
            state.Generation++;
            state.Staged = restoreStaged ? adoption.Candidate : null;
            deleteCandidate = !restoreStaged;
        }

        if (deleteCandidate)
        {
            TryDeleteUnlessCurrent(
                adoption.Current.Seed.FilePath,
                adoption.Previous.Seed.FilePath);
        }
    }

    public void CompleteAdoption(
        string gatewayName,
        NitroSeedAdoption adoption)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);
        ArgumentNullException.ThrowIfNull(adoption);

        var deletePrevious = false;
        lock (_sync)
        {
            deletePrevious = _statesByGateway.TryGetValue(gatewayName, out var state)
                && state.Generation == adoption.Current.Generation
                && string.Equals(
                    state.Current.FilePath,
                    adoption.Current.Seed.FilePath,
                    StringComparison.Ordinal);
        }

        if (deletePrevious)
        {
            TryDeleteUnlessCurrent(
                adoption.Previous.Seed.FilePath,
                adoption.Current.Seed.FilePath);
        }
    }

    public NitroSeedCandidate? GetStagedCandidate(string gatewayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);

        lock (_sync)
        {
            return _statesByGateway.GetValueOrDefault(gatewayName)?.Staged;
        }
    }

    public NitroSeedCandidate? DiscardStagedCandidate(string gatewayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayName);

        lock (_sync)
        {
            if (!_statesByGateway.TryGetValue(gatewayName, out var state))
            {
                return null;
            }

            var staged = state.Staged;
            state.Staged = null;
            return staged;
        }
    }

    public void DeleteCandidate(NitroSeedCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        TryDelete(candidate.Seed.FilePath);
    }

    /// <summary>
    /// Deletes the private copies of this run.
    /// </summary>
    public void DeleteRunSeeds()
    {
        lock (_sync)
        {
            _statesByGateway.Clear();
        }

        try
        {
            if (Directory.Exists(_runSeedDirectory))
            {
                Directory.Delete(_runSeedDirectory, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A directory that cannot be deleted is left for the operating system to clean up.
        }
    }

    private string CopyToRunDirectory(string gatewayName, string seedFilePath)
    {
        var filePath = CreateRunSeedPath(gatewayName);

        File.Copy(seedFilePath, filePath, overwrite: true);

        return filePath;
    }

    private async Task<string> CreateInitialSeedAsync(
        string gatewayName,
        CompositionSettings? settings,
        CancellationToken cancellationToken)
    {
        var filePath = CreateRunSeedPath(gatewayName);

        using var archive = FusionArchive.Create(filePath);
        await archive.SetArchiveMetadataAsync(
            new ArchiveMetadata
            {
                SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
                SourceSchemas = []
            },
            cancellationToken);
        await SetCompositionSettingsAsync(
            archive,
            settings ?? new CompositionSettings(),
            cancellationToken);
        using var gatewaySettings = JsonDocument.Parse(
            """
            {
              "sourceSchemas": {}
            }
            """);
        await archive.SetGatewayConfigurationAsync(
            "type Query { _empty: String }" + Environment.NewLine,
            gatewaySettings,
            WellKnownVersions.LatestGatewayFormatVersion,
            cancellationToken);
        await archive.CommitAsync(cancellationToken);

        return filePath;
    }

    private static async Task ApplyCompositionSettingsAsync(
        string filePath,
        CompositionSettings? settings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (settings is null)
        {
            return;
        }

        using var archive = FusionArchive.Open(filePath, FusionArchiveMode.Update);

        if (archive.IsSigned)
        {
            await archive.RemoveSignatureAsync(cancellationToken);
            logger.LogWarning(
                "The Fusion archive signature was removed before applying Nitro composition settings. "
                + "The producer must re-sign the archive.");
        }

        using var settingsDocument = await archive.GetCompositionSettingsAsync(cancellationToken);
        var existing = settingsDocument?.Deserialize(
            SettingsJsonSerializerContext.Default.CompositionSettings)
            ?? new CompositionSettings();

        await SetCompositionSettingsAsync(
            archive,
            settings.MergeInto(existing),
            cancellationToken);
        await archive.CommitAsync(cancellationToken);
    }

    private static async Task SetCompositionSettingsAsync(
        FusionArchive archive,
        CompositionSettings settings,
        CancellationToken cancellationToken)
    {
        using var settingsDocument = JsonSerializer.SerializeToDocument(
            settings,
            SettingsJsonSerializerContext.Default.CompositionSettings);
        await archive.SetCompositionSettingsAsync(settingsDocument, cancellationToken);
    }

    private string CreateRunSeedPath(string gatewayName)
    {
        Directory.CreateDirectory(_runSeedDirectory);

        return IOPath.Combine(
            _runSeedDirectory,
            $"{gatewayName}.{Interlocked.Increment(ref _nextRunSeedId):D8}.far");
    }

    private static async Task<string> ComputeSchemaHashAsync(
        string archivePath,
        CancellationToken cancellationToken)
    {
        try
        {
            using var archive = FusionArchive.Open(archivePath);
            using var configuration = await archive.TryGetGatewayConfigurationAsync(
                WellKnownVersions.LatestGatewayFormatVersion,
                cancellationToken);

            if (configuration is not null)
            {
                await using var schema = await configuration.OpenReadSchemaAsync(cancellationToken);
                return Convert.ToHexString(await SHA256.HashDataAsync(schema, cancellationToken));
            }
        }
        catch (Exception exception) when (
            exception is IOException or InvalidDataException or InvalidOperationException)
        {
            // A seed produced by an older Nitro version can contain only source configurations.
            // Hashing the complete immutable archive still gives the refresh path a stable guard.
        }

        await using var archiveStream = File.OpenRead(archivePath);
        return Convert.ToHexString(await SHA256.HashDataAsync(archiveStream, cancellationToken));
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteUnlessCurrent(string? filePath, string currentFilePath)
    {
        if (filePath is not null
            && !string.Equals(filePath, currentFilePath, StringComparison.Ordinal))
        {
            TryDelete(filePath);
        }
    }

    private sealed class GatewaySeedState(NitroGatewaySeed current, bool autoUpdate)
    {
        public NitroGatewaySeed Current { get; set; } = current;

        public NitroSeedCandidate? Staged { get; set; }

        public long Generation { get; set; } = 1;

        public bool AutoUpdate { get; set; } = autoUpdate;
    }
}
