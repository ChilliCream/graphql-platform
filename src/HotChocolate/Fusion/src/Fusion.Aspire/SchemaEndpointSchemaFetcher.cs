using System.Net;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire;

internal static class SchemaEndpointSchemaFetcher
{
    public const int DefaultMaxRetries = 15;

    public static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(2);

    public static async Task<string?> FetchAsync(
        string sourceSchemaName,
        Uri endpoint,
        SchemaEndpointProtocol protocol,
        HttpClient httpClient,
        int maxRetries,
        TimeSpan retryDelay,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceSchemaName);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRetries, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(retryDelay, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(logger);

        logger.LogDebug("Waiting for schema service {SourceSchemaName}", sourceSchemaName);

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                if (protocol is SchemaEndpointProtocol.ApolloFederation)
                {
                    return await ApolloFederationSchemaFetcher.FetchAsync(
                        httpClient,
                        sourceSchemaName,
                        endpoint,
                        cancellationToken);
                }

                return await DefaultSchemaFetcher.FetchAsync(
                    httpClient,
                    sourceSchemaName,
                    endpoint,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug(
                    "Schema service {SourceSchemaName} timed out (attempt {Attempt}/{MaxRetries})",
                    sourceSchemaName,
                    i + 1,
                    maxRetries);
            }
            catch (HttpRequestException exception) when (exception.StatusCode is null)
            {
                logger.LogDebug(
                    "Schema service {SourceSchemaName} was unavailable (attempt {Attempt}/{MaxRetries})",
                    sourceSchemaName,
                    i + 1,
                    maxRetries);
            }
            catch (IOException)
            {
                logger.LogDebug(
                    "Schema service {SourceSchemaName} was unavailable (attempt {Attempt}/{MaxRetries})",
                    sourceSchemaName,
                    i + 1,
                    maxRetries);
            }
            // The DCP proxy keeps an endpoint open while the source process starts or
            // restarts, so a fetch can observe transient server errors instead of
            // connection failures. Server errors are retried, everything below 500
            // fails immediately.
            catch (HttpRequestException exception) when (
                exception.StatusCode >= HttpStatusCode.InternalServerError)
            {
                logger.LogDebug(
                    "Schema service {SourceSchemaName} returned a transient server error (attempt {Attempt}/{MaxRetries})",
                    sourceSchemaName,
                    i + 1,
                    maxRetries);
            }
            catch (SchemaFetchRequestException exception) when (
                exception.StatusCode >= HttpStatusCode.InternalServerError)
            {
                logger.LogDebug(
                    "Schema service {SourceSchemaName} returned a transient server error (attempt {Attempt}/{MaxRetries})",
                    sourceSchemaName,
                    i + 1,
                    maxRetries);
            }

            if (i + 1 < maxRetries)
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        logger.LogWarning(
            "Schema service {SourceSchemaName} failed to become ready after {MaxRetries} attempts",
            sourceSchemaName,
            maxRetries);
        return null;
    }
}
