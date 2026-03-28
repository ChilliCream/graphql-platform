using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Exceptions;
using StrawberryShake;

namespace ChilliCream.Nitro.Client.Clients;

internal sealed class ClientsClient(
    IApiClient apiClient,
    IHttpClientFactory httpClientFactory)
    : IClientsClient
{
    public async Task<ICreateClientCommandMutation_CreateClient> CreateClientAsync(
        string apiId,
        string name,
        CancellationToken cancellationToken)
    {
        var input = new CreateClientInput
        {
            ApiId = apiId,
            Name = name
        };

        var result = await apiClient.CreateClientCommandMutation.ExecuteAsync(input, cancellationToken);
        return OperationResultHelper.EnsureData(result).CreateClient;
    }

    public async Task<IDeleteClientByIdCommandMutation_DeleteClientById> DeleteClientAsync(
        string clientId,
        CancellationToken cancellationToken)
    {
        var input = new DeleteClientByIdInput
        {
            ClientId = clientId
        };

        var result = await apiClient.DeleteClientByIdCommandMutation.ExecuteAsync(input, cancellationToken);
        return OperationResultHelper.EnsureData(result).DeleteClientById;
    }

    public async Task<IShowClientCommandQuery_Node?> ShowClientAsync(
        string clientId,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ShowClientCommandQuery.ExecuteAsync(clientId, cancellationToken);
        return OperationResultHelper.EnsureData(result).Node;
    }

    public async Task<ConnectionPage<IListClientCommandQuery_Node_Clients_Edges_Node>> ListClientsAsync(
        string apiId,
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListClientCommandQuery.ExecuteAsync(
            apiId,
            after,
            first,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        var connection = (data.Node as IListClientCommandQuery_Node_Api)?.Clients;
        if (connection is null)
        {
            throw new NitroClientException($"Failed to list clients: API '{apiId}' was not found.");
        }

        var items = connection.Edges?.Select(static edge => edge.Node).ToArray() ?? [];

        return new ConnectionPage<IListClientCommandQuery_Node_Clients_Edges_Node>(
            items,
            connection.PageInfo.EndCursor,
            connection.PageInfo.HasNextPage);
    }

    public async Task<IUploadClient_UploadClient> UploadClientVersionAsync(
        string clientId,
        string tag,
        Stream operationsStream,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new UploadClientInput
        {
            ClientId = clientId,
            Tag = tag,
            Operations = new Upload(operationsStream, "operations.graphql"),
            Source = SourceMetadataMapper.Map(source)
        };

        var result = await apiClient.UploadClient.ExecuteAsync(input, cancellationToken);
        return OperationResultHelper.EnsureData(result).UploadClient;
    }

    public async Task<IValidateClientVersion_ValidateClient> StartClientValidationAsync(
        string clientId,
        string stageName,
        Stream operationsStream,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new ValidateClientInput
        {
            ClientId = clientId,
            Stage = stageName,
            Operations = new Upload(operationsStream, "operations.graphql"),
            Source = SourceMetadataMapper.Map(source)
        };

        var result = await apiClient.ValidateClientVersion.ExecuteAsync(input, cancellationToken);
        return OperationResultHelper.EnsureData(result).ValidateClient;
    }

    public async IAsyncEnumerable<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate> SubscribeToClientValidationAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stopSignal = new ReplaySubject<Unit>(1);
        await using var _ = cancellationToken.Register(stopSignal);

        var subscription = apiClient.OnClientVersionValidationUpdated
            .Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        // The cancellation token is intentionally not passed to ToAsyncEnumerable() to avoid
        // an OperationCanceledException. Cancellation is handled via the stop signal above,
        // which completes the sequence cleanly.
        await foreach (var update in subscription.ToAsyncEnumerable())
        {
            var data = OperationResultHelper.EnsureData(update);

            yield return data.OnClientVersionValidationUpdate;
        }
    }

    public async Task<IPublishClientVersion_PublishClient> StartClientPublishAsync(
        string clientId,
        string stageName,
        string tag,
        bool force,
        bool waitForApproval,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new PublishClientInput
        {
            ClientId = clientId,
            Stage = stageName,
            Tag = tag,
            WaitForApproval = waitForApproval,
            Source = SourceMetadataMapper.Map(source)
        };

        if (force)
        {
            input = input with { Force = true };
        }

        var result = await apiClient.PublishClientVersion.ExecuteAsync(input, cancellationToken);
        return OperationResultHelper.EnsureData(result).PublishClient;
    }

    public async IAsyncEnumerable<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate> SubscribeToClientPublishAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stopSignal = new ReplaySubject<Unit>(1);
        await using var _ = cancellationToken.Register(stopSignal);

        var subscription = apiClient.OnClientVersionPublishUpdated
            .Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        // The cancellation token is intentionally not passed to ToAsyncEnumerable() to avoid
        // an OperationCanceledException. Cancellation is handled via the stop signal above,
        // which completes the sequence cleanly.
        await foreach (var update in subscription.ToAsyncEnumerable())
        {
            var data = OperationResultHelper.EnsureData(update);

            yield return data.OnClientVersionPublishingUpdate;
        }
    }

    public async Task<IUnpublishClient_UnpublishClient> UnpublishClientVersionAsync(
        string clientId,
        string stageName,
        string tag,
        CancellationToken cancellationToken)
    {
        var input = new UnpublishClientInput
        {
            ClientId = clientId,
            Stage = stageName,
            Tag = tag
        };

        var result = await apiClient.UnpublishClient.ExecuteAsync(input, cancellationToken);
        return OperationResultHelper.EnsureData(result).UnpublishClient;
    }

    public async Task<Stream?> DownloadPersistedQueriesAsync(
        string apiId,
        string stageName,
        CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(ApiClient.ClientName);

        var encodedApiId = Uri.EscapeDataString(apiId);
        var encodedStageName = Uri.EscapeDataString(stageName);

        using var response = await httpClient.GetAsync(
            $"/api/v1/apis/{encodedApiId}/persistedQueries?stage={encodedStageName}",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new NitroClientAuthorizationException(
                $"Got a HTTP {response.StatusCode} while attempting to download persisted queries.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new NitroClientException(
                $"Failed to download persisted queries: server returned {(int)response.StatusCode} ({response.StatusCode}).");
        }

        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task<ConnectionPage<IClientDetailPrompt_ClientVersionEdge>> ListClientVersionsAsync(
        string clientId,
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        if (after is null)
        {
            var result = await apiClient.ShowClientCommandQuery.ExecuteAsync(clientId, cancellationToken);
            var data = OperationResultHelper.EnsureData(result);

            if (data.Node is not IShowClientCommandQuery_Node_Client client)
            {
                throw new NitroClientException(
                    $"Failed to list client versions: client '{clientId}' was not found.");
            }

            return MapVersionPage(
                client.Versions?.Edges,
                client.Versions?.PageInfo.EndCursor,
                client.Versions?.PageInfo.HasNextPage ?? false);
        }

        var pageResult = await apiClient.PageClientVersionDetailQuery.ExecuteAsync(
            clientId,
            after,
            cancellationToken);

        var pageData = OperationResultHelper.EnsureData(pageResult);

        if (pageData.Node is not IPageClientVersionDetailQuery_Node_Client pageClient)
        {
            throw new NitroClientException(
                $"Failed to list client versions: client '{clientId}' was not found.");
        }

        return MapVersionPage(
            pageClient.Versions?.Edges,
            pageClient.Versions?.PageInfo.EndCursor,
            pageClient.Versions?.PageInfo.HasNextPage ?? false);
    }

    private static ConnectionPage<IClientDetailPrompt_ClientVersionEdge> MapVersionPage(
        IEnumerable<IClientDetailPrompt_ClientVersionEdge>? edges,
        string? endCursor,
        bool hasNextPage)
    {
        var items = edges?.ToArray() ?? [];

        return new ConnectionPage<IClientDetailPrompt_ClientVersionEdge>(
            items,
            endCursor,
            hasNextPage);
    }
}
