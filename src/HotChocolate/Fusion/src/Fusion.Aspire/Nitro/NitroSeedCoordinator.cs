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
    private readonly Dictionary<string, NitroGatewaySeed> _seedsByGateway = [with(StringComparer.Ordinal)];
    private readonly Lock _sync = new();
    private readonly NitroConnectionResolver _connectionResolver;
    private readonly NitroSeedProvider _seedProvider;
    private readonly INitroSchemaValidator _schemaValidator;
    private readonly string _runSeedDirectory;

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
    /// <param name="runSeedDirectory">
    /// The directory that holds the private copies of this run.
    /// </param>
    public NitroSeedCoordinator(
        string stage,
        NitroConnectionResolver connectionResolver,
        NitroSeedProvider seedProvider,
        INitroSchemaValidator schemaValidator,
        string runSeedDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentNullException.ThrowIfNull(connectionResolver);
        ArgumentNullException.ThrowIfNull(seedProvider);
        ArgumentNullException.ThrowIfNull(schemaValidator);
        ArgumentException.ThrowIfNullOrWhiteSpace(runSeedDirectory);

        Stage = stage;
        _connectionResolver = connectionResolver;
        _seedProvider = seedProvider;
        _schemaValidator = schemaValidator;
        _runSeedDirectory = runSeedDirectory;
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
    public static NitroSeedCoordinator CreateProduction(string stage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);

        var timeProvider = TimeProvider.System;
        var httpClient = new HttpClient();
        var connectionResolver = new NitroConnectionResolver(
            new NitroSessionReader(
                NitroDefaults.GetSessionFilePath(),
                NitroDefaults.SessionRereadDelay),
            SystemNitroEnvironment.Instance,
            NitroDefaults.ApiUrl,
            timeProvider,
            NitroDefaults.AccessTokenExpiryGrace);
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

        return new NitroSeedCoordinator(
            stage,
            connectionResolver,
            seedProvider,
            schemaValidator,
            NitroDefaults.CreateRunSeedDirectoryPath());
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
        var result = await _seedProvider.GetSeedAsync(
            connection,
            apiId,
            Stage,
            logger,
            cancellationToken);

        if (result.Outcome is NitroSeedOutcome.Unavailable)
        {
            return NitroSeedAcquisition.Failed(result.Message!);
        }

        var seed = new NitroGatewaySeed(
            apiId,
            Stage,
            CopyToRunDirectory(gatewayName, result.FilePath!),
            result.DownloadedAt!.Value,
            result.Outcome is NitroSeedOutcome.Downloaded);

        lock (_sync)
        {
            _seedsByGateway[gatewayName] = seed;
        }

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
            return _seedsByGateway.GetValueOrDefault(gatewayName);
        }
    }

    /// <summary>
    /// Deletes the private copies of this run.
    /// </summary>
    public void DeleteRunSeeds()
    {
        lock (_sync)
        {
            _seedsByGateway.Clear();
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
        Directory.CreateDirectory(_runSeedDirectory);

        var filePath = IOPath.Combine(_runSeedDirectory, gatewayName + ".far");

        File.Copy(seedFilePath, filePath, overwrite: true);

        return filePath;
    }
}
