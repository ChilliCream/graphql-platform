using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Resolves a Nitro api by its id through the Nitro GraphQL API. The lookup exists only to turn
/// a failed fusion configuration download into a message that says whether the api id is wrong
/// or whether the api has no fusion configuration for the requested stage, so it never runs on
/// the happy path and never fails the caller.
/// </summary>
internal sealed class NitroApiLookupClient
{
    private readonly GraphQLHttpClient _client;

    /// <summary>
    /// Initializes a new instance of <see cref="NitroApiLookupClient"/>.
    /// </summary>
    /// <param name="client">
    /// The GraphQL client that sends the lookup.
    /// </param>
    public NitroApiLookupClient(GraphQLHttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        _client = client;
    }

    /// <summary>
    /// Resolves the name of a Nitro api by its id.
    /// </summary>
    /// <param name="connection">
    /// The connection to the Nitro API.
    /// </param>
    /// <param name="apiId">
    /// The id of the api.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a warning when the lookup fails.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public async Task<NitroApiLookupResult> ResolveApiAsync(
        NitroConnection connection,
        string apiId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            var request = new GraphQLHttpRequest(
#if NITRO_PERSISTED_OPERATIONS
                new OperationRequest(
                    id: NitroOperationDocuments.GetResolveApiNameOperationId(),
                    operationName: NitroOperationDocuments.ResolveApiNameOperationName,
                    variables: new Dictionary<string, object?> { ["id"] = apiId }),
#else
                new OperationRequest(
                    NitroOperationDocuments.GetResolveApiNameDocument(),
                    operationName: NitroOperationDocuments.ResolveApiNameOperationName,
                    variables: new Dictionary<string, object?> { ["id"] = apiId }),
#endif
                connection.GraphQLEndpoint)
            {
                OnMessageCreated = (_, requestMessage, _) =>
                    NitroRequestHeaders.Apply(requestMessage, connection.Credential)
            };

            using var response = await _client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "The Nitro api lookup for the api id {ApiId} failed with the status code "
                    + "{StatusCode}.",
                    apiId,
                    (int)response.StatusCode);

                return NitroApiLookupResult.Failed;
            }

            using var result = await response.ReadAsResultAsync(cancellationToken);

            if (result.Errors.ValueKind is JsonValueKind.Array && result.Errors.GetArrayLength() > 0)
            {
                logger.LogWarning(
                    "The Nitro api lookup for the api id {ApiId} returned errors: {Errors}",
                    apiId,
                    result.Errors.ToString());

                return NitroApiLookupResult.Failed;
            }

            if (result.Data.ValueKind is not JsonValueKind.Object
                || !result.Data.TryGetProperty("node", out var node))
            {
                logger.LogWarning(
                    "The Nitro api lookup for the api id {ApiId} returned an unexpected result.",
                    apiId);

                return NitroApiLookupResult.Failed;
            }

            if (node.ValueKind is JsonValueKind.Null)
            {
                return NitroApiLookupResult.NotFound;
            }

            if (node.ValueKind is JsonValueKind.Object
                && node.TryGetProperty("name", out var name)
                && name.ValueKind is JsonValueKind.String)
            {
                return NitroApiLookupResult.Found(name.GetString()!);
            }

            logger.LogWarning(
                "The Nitro api lookup for the api id {ApiId} returned a node that is not an api.",
                apiId);

            return NitroApiLookupResult.Failed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "The Nitro api lookup for the api id {ApiId} could not be sent to {Endpoint}.",
                apiId,
                connection.GraphQLEndpoint);

            return NitroApiLookupResult.Failed;
        }
    }
}
