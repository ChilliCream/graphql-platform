using System.Net.Http.Headers;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal sealed class NitroStageUpdateClient(GraphQLHttpClient client)
    : INitroStageUpdateClient
{
    private const string EventStreamMediaType = "text/event-stream";

    public async Task<NitroStageSubscription> SubscribeAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);

        // Fusion configuration publishes change the composition base. Client publish, unpublish,
        // and delete events change the registered operations carried by the downloaded archive.
        // The operation deliberately filters out OpenAPI and MCP changes, which do not affect a
        // Fusion gateway archive.
        var request = CreateRequest(
            connection,
            NitroOperationDocuments.WatchStageOperationName,
#if NITRO_PERSISTED_OPERATIONS
            NitroOperationDocuments.GetWatchStageOperationId(),
#else
            NitroOperationDocuments.GetWatchStageDocument(),
#endif
            apiId,
            stage);
        request.Accept = GraphQLHttpRequest.GraphQLOverSse;
        request.OperationKind = OperationType.Subscription;

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode
            || !string.Equals(
                response.ContentHeaders.ContentType?.MediaType,
                EventStreamMediaType,
                StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                response.EnsureSuccessStatusCode();
                throw new InvalidOperationException(
                    "Nitro did not establish the stage-change SSE subscription.");
            }
            finally
            {
                response.Dispose();
            }
        }

        return new HttpNitroStageSubscription(response);
    }

    public async Task<NitroStageSnapshot?> GetLatestSnapshotAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);

        var request = CreateRequest(
            connection,
            NitroOperationDocuments.GetStageVersionOperationName,
#if NITRO_PERSISTED_OPERATIONS
            NitroOperationDocuments.GetStageVersionOperationId(),
#else
            NitroOperationDocuments.GetStageVersionDocument(),
#endif
            apiId,
            stage);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var result = await response.ReadAsResultAsync(cancellationToken);
        ThrowOnErrors(result);

        if (result.Data.ValueKind is not JsonValueKind.Object
            || !result.Data.TryGetProperty("apiById", out var api)
            || api.ValueKind is not JsonValueKind.Object
            || !api.TryGetProperty("stage", out var stageElement)
            || stageElement.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        if (stageElement.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidDataException("Nitro returned a malformed stage version.");
        }

        var fusionConfigurationId = ReadNestedId(
            stageElement,
            "publishedFusionConfiguration");
        var clientVersions = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        if (stageElement.TryGetProperty("publishedClients", out var clients)
            && clients.ValueKind is JsonValueKind.Array)
        {
            foreach (var publishedClient in clients.EnumerateArray())
            {
                var clientId = ReadNestedId(publishedClient, "client");
                if (clientId is null)
                {
                    continue;
                }

                var versions = new HashSet<string>(StringComparer.Ordinal);
                if (publishedClient.TryGetProperty("publishedVersions", out var publishedVersions)
                    && publishedVersions.ValueKind is JsonValueKind.Array)
                {
                    foreach (var publishedVersion in publishedVersions.EnumerateArray())
                    {
                        if (ReadNestedId(publishedVersion, "version") is { } versionId)
                        {
                            versions.Add(versionId);
                        }
                    }
                }

                if (versions.Count > 0)
                {
                    clientVersions[clientId] = versions;
                }
            }
        }

        return new NitroStageSnapshot(fusionConfigurationId, clientVersions);
    }

    private static GraphQLHttpRequest CreateRequest(
        NitroConnection connection,
        string operationName,
        string documentOrId,
        string apiId,
        string stage)
    {
        var variables = new Dictionary<string, object?>
        {
            ["apiId"] = apiId,
            ["stageName"] = stage
        };

#if NITRO_PERSISTED_OPERATIONS
        var body = new OperationRequest(
            id: documentOrId,
            operationName: operationName,
            variables: variables);
#else
        var body = new OperationRequest(
            documentOrId,
            operationName: operationName,
            variables: variables);
#endif
        return new GraphQLHttpRequest(body, connection.GraphQLEndpoint)
        {
            OnMessageCreated = (_, requestMessage, _) =>
                NitroRequestHeaders.Apply(requestMessage, connection.Credential)
        };
    }

    private static void ThrowOnErrors(OperationResult result)
    {
        if (result.Errors.ValueKind is JsonValueKind.Array
            && result.Errors.GetArrayLength() > 0)
        {
            throw new InvalidDataException("Nitro returned GraphQL errors for a stage update operation.");
        }
    }

    private static string? ReadNestedId(JsonElement parent, string propertyName)
        => parent.TryGetProperty(propertyName, out var value)
            && value.ValueKind is JsonValueKind.Object
            && value.TryGetProperty("id", out var id)
            && id.ValueKind is JsonValueKind.String
                ? id.GetString()
                : null;

    private sealed class HttpNitroStageSubscription(GraphQLHttpResponse response)
        : NitroStageSubscription
    {
        public override async IAsyncEnumerable<NitroStageChange> ReadChangesAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken)
        {
            await foreach (var result in response.ReadAsResultStreamAsync()
                .WithCancellation(cancellationToken))
            {
                using (result)
                {
                    ThrowOnErrors(result);

                    if (TryParseChange(result.Data) is { } change)
                    {
                        yield return change;
                    }
                }
            }
        }

        public override ValueTask DisposeAsync()
        {
            response.Dispose();
            return ValueTask.CompletedTask;
        }

        private static NitroStageChange? TryParseChange(JsonElement data)
        {
            if (data.ValueKind is not JsonValueKind.Object
                || !data.TryGetProperty("onStageChanged", out var eventElement)
                || eventElement.ValueKind is not JsonValueKind.Object
                || !eventElement.TryGetProperty("__typename", out var typeNameElement)
                || typeNameElement.ValueKind is not JsonValueKind.String)
            {
                throw new InvalidDataException("Nitro returned a malformed stage-change event.");
            }

            return typeNameElement.GetString() switch
            {
                "FusionConfigurationPublishedStageChangeEvent" => new NitroStageChange(
                    NitroStageChangeKind.FusionConfigurationPublished,
                    FusionConfigurationId: ReadNestedId(
                        eventElement,
                        "fusionConfiguration")),
                "ClientVersionPublishedStageChangeEvent" => CreateClientVersionChange(
                    eventElement,
                    NitroStageChangeKind.ClientVersionPublished),
                "ClientVersionUnpublishedStageChangeEvent" => CreateClientVersionChange(
                    eventElement,
                    NitroStageChangeKind.ClientVersionUnpublished),
                "ClientDeletedStageChangeEvent" => new NitroStageChange(
                    NitroStageChangeKind.ClientDeleted,
                    ClientId: ReadString(eventElement, "clientId")),
                _ => null
            };
        }

        private static NitroStageChange CreateClientVersionChange(
            JsonElement eventElement,
            NitroStageChangeKind kind)
        {
            if (!eventElement.TryGetProperty("clientVersion", out var clientVersion)
                || clientVersion.ValueKind is not JsonValueKind.Object)
            {
                return new NitroStageChange(kind);
            }

            return new NitroStageChange(
                kind,
                ClientId: ReadNestedId(clientVersion, "client"),
                ClientVersionId: ReadString(clientVersion, "id"));
        }

        private static string? ReadString(JsonElement parent, string propertyName)
            => parent.TryGetProperty(propertyName, out var value)
                && value.ValueKind is JsonValueKind.String
                    ? value.GetString()
                    : null;
    }
}
