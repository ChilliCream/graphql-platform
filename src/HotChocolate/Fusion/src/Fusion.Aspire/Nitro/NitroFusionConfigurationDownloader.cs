using System.Net;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Downloads the latest fusion archive of a Nitro api for a stage.
/// </summary>
/// <remarks>
/// Rejected credentials, unknown apis and missing configurations are terminal and are never
/// retried. Server errors and connection level errors are retried within the configured budget.
/// </remarks>
internal sealed class NitroFusionConfigurationDownloader
{
    private readonly HttpClient _httpClient;
    private readonly NitroDownloadRetryPolicy _retryPolicy;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="NitroFusionConfigurationDownloader"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client that sends the download request.
    /// </param>
    /// <param name="retryPolicy">
    /// The retry budget for the download.
    /// </param>
    /// <param name="timeProvider">
    /// The time source that the delay between two attempts is taken from.
    /// </param>
    public NitroFusionConfigurationDownloader(
        HttpClient httpClient,
        NitroDownloadRetryPolicy retryPolicy,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _httpClient = httpClient;
        _retryPolicy = retryPolicy;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Downloads the latest fusion archive of an api for a stage into
    /// <paramref name="destination"/>.
    /// </summary>
    /// <param name="connection">
    /// The connection to the Nitro API.
    /// </param>
    /// <param name="apiId">
    /// The id of the api that carries the fusion configuration.
    /// </param>
    /// <param name="stage">
    /// The name of the stage whose fusion configuration is downloaded.
    /// </param>
    /// <param name="hasCachedSeed">
    /// Whether a cached fusion configuration exists, which selects the retry budget.
    /// </param>
    /// <param name="destination">
    /// The stream that the archive is written to. It is truncated before every attempt, so it
    /// only carries a complete payload when the result is
    /// <see cref="NitroDownloadStatus.Success"/>.
    /// </param>
    /// <param name="logger">
    /// The logger that receives one message per failed attempt.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public async Task<NitroDownloadResult> DownloadAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        bool hasCachedSeed,
        Stream destination,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(logger);

        var requestUri = NitroApiUrl.CreateFusionConfigurationDownloadUrl(
            connection.ApiUrl,
            apiId,
            stage,
            WellKnownVersions.LatestGatewayFormatVersion);

        var attempts = _retryPolicy.GetAttempts(hasCachedSeed);
        var result = default(NitroDownloadResult);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            var (status, statusCode, message) =
                await SendAsync(requestUri, connection, destination, cancellationToken);

            result = new NitroDownloadResult(status, statusCode, attempt, message);

            if (status is not NitroDownloadStatus.TransientExhausted)
            {
                return result;
            }

            logger.LogWarning(
                "Attempt {Attempt} of {Attempts} to download the fusion configuration for the "
                + "api {ApiId} and the stage {Stage} from {RequestUri} failed. {Message}",
                attempt,
                attempts,
                apiId,
                stage,
                requestUri,
                message);

            if (attempt < attempts)
            {
                await Task.Delay(_retryPolicy.Delay, _timeProvider, cancellationToken);
            }
        }

        return result!;
    }

    private async Task<AttemptResult> SendAsync(
        Uri requestUri,
        NitroConnection connection,
        Stream destination,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        NitroRequestHeaders.Apply(request, connection.Credential);

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return Failure(
                    NitroDownloadStatus.Unauthorized,
                    response.StatusCode,
                    $"Nitro rejected the request with the status code {(int)response.StatusCode}.");
            }

            if (response.StatusCode is HttpStatusCode.NotFound)
            {
                return Failure(
                    NitroDownloadStatus.NotFound,
                    response.StatusCode,
                    "Nitro returned the status code 404.");
            }

            if ((int)response.StatusCode >= 500)
            {
                return Failure(
                    NitroDownloadStatus.TransientExhausted,
                    response.StatusCode,
                    $"Nitro returned the status code {(int)response.StatusCode}.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return Failure(
                    NitroDownloadStatus.PermanentFailure,
                    response.StatusCode,
                    $"Nitro returned the status code {(int)response.StatusCode}.");
            }

            Truncate(destination);

            await response.Content.CopyToAsync(destination, cancellationToken);
            await destination.FlushAsync(cancellationToken);

            return new AttemptResult(
                NitroDownloadStatus.Success,
                response.StatusCode,
                Message: null);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is null)
        {
            return Failure(NitroDownloadStatus.TransientExhausted, statusCode: null, ex.Message);
        }
        catch (IOException ex)
        {
            return Failure(NitroDownloadStatus.TransientExhausted, statusCode: null, ex.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            return Failure(NitroDownloadStatus.TransientExhausted, statusCode: null, ex.Message);
        }
    }

    private static void Truncate(Stream destination)
    {
        if (destination.CanSeek)
        {
            destination.SetLength(0);
            destination.Position = 0;
        }
    }

    private static AttemptResult Failure(
        NitroDownloadStatus status,
        HttpStatusCode? statusCode,
        string message)
        => new(status, statusCode, message);

    private sealed record AttemptResult(
        NitroDownloadStatus Status,
        HttpStatusCode? StatusCode,
        string? Message);
}
