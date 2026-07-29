using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Provides the fusion configuration that a gateway composes against.
/// </summary>
/// <remarks>
/// A fresh fusion configuration is downloaded and cached. When it cannot be fetched, for any
/// reason including a missing or expired sign-in, a cached fusion configuration is used and the
/// gateway is warned that the configuration is not fresh. Only when no cached fusion
/// configuration exists does the result become
/// <see cref="NitroSeedOutcome.Unavailable"/>, which fails the gateway.
/// </remarks>
internal sealed class NitroSeedProvider
{
    private readonly NitroFusionConfigurationDownloader _downloader;
    private readonly NitroSeedCache _cache;
    private readonly NitroApiLookupClient _apiLookupClient;

    /// <summary>
    /// Initializes a new instance of <see cref="NitroSeedProvider"/>.
    /// </summary>
    /// <param name="downloader">
    /// The downloader for fusion configurations.
    /// </param>
    /// <param name="cache">
    /// The cache of the last known good fusion configurations.
    /// </param>
    /// <param name="apiLookupClient">
    /// The client that resolves an api by its id to word a failed download.
    /// </param>
    public NitroSeedProvider(
        NitroFusionConfigurationDownloader downloader,
        NitroSeedCache cache,
        NitroApiLookupClient apiLookupClient)
    {
        ArgumentNullException.ThrowIfNull(downloader);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(apiLookupClient);

        _downloader = downloader;
        _cache = cache;
        _apiLookupClient = apiLookupClient;
    }

    /// <summary>
    /// Gets the fusion configuration of an api for a stage.
    /// </summary>
    /// <param name="connection">
    /// The connection to the Nitro API.
    /// </param>
    /// <param name="apiId">
    /// The id of the api that carries the fusion configuration.
    /// </param>
    /// <param name="stage">
    /// The name of the stage whose fusion configuration is used.
    /// </param>
    /// <param name="logger">
    /// The logger of the gateway that composes against the fusion configuration. It receives the
    /// warning when the configuration is not fresh.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public async Task<NitroSeedResult> GetSeedAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentNullException.ThrowIfNull(logger);

        var key = new NitroSeedKey(connection.ApiUrl, apiId, stage);
        var cached = await _cache.TryGetAsync(key, logger, cancellationToken);

        if (connection.Credential.Kind is NitroCredentialKind.None)
        {
            return FallBack(cached, connection.Credential.UnavailableMessage!, logger);
        }

        var tempFilePath = _cache.CreateTempFilePath(key);

        try
        {
            NitroDownloadResult download;

            await using (var destination = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                download = await _downloader.DownloadAsync(
                    connection,
                    apiId,
                    stage,
                    cached is not null,
                    destination,
                    logger,
                    cancellationToken);
            }

            if (download.Status is NitroDownloadStatus.Success)
            {
                var promoted = await _cache.TryPromoteAsync(
                    key,
                    tempFilePath,
                    logger,
                    cancellationToken);

                if (promoted is not null)
                {
                    logger.LogInformation(
                        "Downloaded the fusion configuration for the api {ApiId} and the stage "
                        + "{Stage} from {ApiUrl}.",
                        apiId,
                        stage,
                        connection.ApiUrl);

                    return new NitroSeedResult(
                        NitroSeedOutcome.Downloaded,
                        promoted.FilePath,
                        promoted.DownloadedAt,
                        Message: null);
                }

                return FallBack(
                    cached,
                    $"The fusion configuration that was downloaded for the api '{apiId}' and the "
                    + $"stage '{stage}' from '{connection.ApiUrl}' is not a valid fusion archive.",
                    logger);
            }

            var message = await DescribeFailureAsync(
                connection,
                apiId,
                stage,
                download,
                logger,
                cancellationToken);

            return FallBack(cached, message, logger);
        }
        finally
        {
            _cache.DeleteTempFile(tempFilePath);
        }
    }

    private static NitroSeedResult FallBack(
        NitroSeedCacheEntry? cached,
        string reason,
        ILogger logger)
    {
        if (cached is null)
        {
            return new NitroSeedResult(
                NitroSeedOutcome.Unavailable,
                FilePath: null,
                DownloadedAt: null,
                reason);
        }

        logger.LogWarning(
            "A fresh fusion configuration could NOT be fetched from Nitro. {Reason} Falling back "
            + "to the fusion configuration that was downloaded at {DownloadedAt}, which may be "
            + "out of date.",
            reason,
            cached.DownloadedAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + "Z");

        return new NitroSeedResult(
            NitroSeedOutcome.ServedFromCache,
            cached.FilePath,
            cached.DownloadedAt,
            reason);
    }

    private async Task<string> DescribeFailureAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        NitroDownloadResult download,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        switch (download.Status)
        {
            case NitroDownloadStatus.Unauthorized:
                return connection.Credential.Kind is NitroCredentialKind.ApiKey
                    ? $"Nitro at '{connection.ApiUrl}' rejected the API key from the "
                        + $"{NitroEnvironmentVariables.ApiKey} environment variable "
                        + $"({download.Message})."
                    : $"Nitro at '{connection.ApiUrl}' rejected the session access token "
                        + $"({download.Message}). Run 'nitro login' to sign in again.";

            case NitroDownloadStatus.NotFound:
                var lookup = await _apiLookupClient.ResolveApiAsync(
                    connection,
                    apiId,
                    logger,
                    cancellationToken);

                return lookup.Status switch
                {
                    NitroApiLookupStatus.Found =>
                        $"The api '{lookup.Name}' with the id '{apiId}' has no fusion "
                        + $"configuration for the stage '{stage}' in Nitro.",
                    NitroApiLookupStatus.NotFound =>
                        $"Nitro at '{connection.ApiUrl}' knows no api with the id '{apiId}'. "
                        + "Check the api id that is passed to WithNitroApiId.",
                    _ =>
                        $"Nitro at '{connection.ApiUrl}' returned no fusion configuration for the "
                        + $"api id '{apiId}' and the stage '{stage}'."
                };

            case NitroDownloadStatus.TransientExhausted:
                return $"The fusion configuration for the api '{apiId}' and the stage '{stage}' "
                    + $"could not be downloaded from '{connection.ApiUrl}' after "
                    + $"{download.Attempts} attempts ({download.Message}).";

            default:
                return $"The fusion configuration for the api '{apiId}' and the stage '{stage}' "
                    + $"could not be downloaded from '{connection.ApiUrl}' ({download.Message}).";
        }
    }
}
