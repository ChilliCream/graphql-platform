using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using StrawberryShake;

namespace ChilliCream.Nitro.Client.Mcp;

internal sealed class McpClient(IApiClient apiClient) : IMcpClient
{
    public async Task<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection> CreateMcpFeatureCollectionAsync(
        string apiId,
        string name,
        CancellationToken cancellationToken)
    {
        var input = new CreateMcpFeatureCollectionInput
        {
            ApiId = apiId,
            Name = name
        };

        var result = await apiClient.CreateMcpFeatureCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).CreateMcpFeatureCollection;
    }

    public async Task<IDeleteMcpFeatureCollectionByIdCommandMutation_DeleteMcpFeatureCollectionById> DeleteMcpFeatureCollectionAsync(
        string mcpFeatureCollectionId,
        CancellationToken cancellationToken)
    {
        var input = new DeleteMcpFeatureCollectionByIdInput
        {
            McpFeatureCollectionId = mcpFeatureCollectionId
        };

        var result = await apiClient.DeleteMcpFeatureCollectionByIdCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).DeleteMcpFeatureCollectionById;
    }

    public async Task<ConnectionPage<IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node>?> ListMcpFeatureCollectionsAsync(
        string apiId,
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListMcpFeatureCollectionCommandQuery.ExecuteAsync(
            apiId,
            after,
            first,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        var connection = (data.Node as IListMcpFeatureCollectionCommandQuery_Node_Api)?.McpFeatureCollections;
        if (connection is null)
        {
            return null;
        }

        var items = connection.Edges?.Select(static edge => edge.Node).ToArray() ?? [];

        return new ConnectionPage<IListMcpFeatureCollectionCommandQuery_Node_McpFeatureCollections_Edges_Node>(
            items,
            connection.PageInfo.EndCursor,
            connection.PageInfo.HasNextPage);
    }

    public async Task<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection> UploadMcpFeatureCollectionVersionAsync(
        string mcpFeatureCollectionId,
        string tag,
        Stream collectionArchiveStream,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new UploadMcpFeatureCollectionInput
        {
            McpFeatureCollectionId = mcpFeatureCollectionId,
            Tag = tag,
            Collection = new Upload(collectionArchiveStream, "collection.zip"),
            Source = SourceMetadataMapper.Map(source)
        };

        var result = await apiClient.UploadMcpFeatureCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).UploadMcpFeatureCollection;
    }

    public async Task<IValidateMcpFeatureCollectionCommandMutation_ValidateMcpFeatureCollection> StartMcpFeatureCollectionValidationAsync(
        string mcpFeatureCollectionId,
        string stageName,
        Stream collectionArchiveStream,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new ValidateMcpFeatureCollectionInput
        {
            McpFeatureCollectionId = mcpFeatureCollectionId,
            Stage = stageName,
            Collection = new Upload(collectionArchiveStream, "collection.zip"),
            Source = SourceMetadataMapper.Map(source)
        };

        var result = await apiClient.ValidateMcpFeatureCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).ValidateMcpFeatureCollection;
    }

    public async IAsyncEnumerable<IValidateMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionValidationUpdate> SubscribeToMcpFeatureCollectionValidationAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stopSignal = new ReplaySubject<Unit>(1);
        await using var _ = cancellationToken.Register(stopSignal);

        var subscription = apiClient.ValidateMcpFeatureCollectionCommandSubscription
            .Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        // The cancellation token is intentionally not passed to ToAsyncEnumerable() to avoid
        // an OperationCanceledException. Cancellation is handled via the stop signal above,
        // which completes the sequence cleanly.
        await foreach (var update in subscription.ToAsyncEnumerable())
        {
            var data = OperationResultHelper.EnsureData(update);

            yield return data.OnMcpFeatureCollectionVersionValidationUpdate;
        }
    }

    public async Task<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection> StartMcpFeatureCollectionPublishAsync(
        string mcpFeatureCollectionId,
        string stageName,
        string tag,
        bool force,
        bool waitForApproval,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new PublishMcpFeatureCollectionInput
        {
            McpFeatureCollectionId = mcpFeatureCollectionId,
            Stage = stageName,
            Tag = tag,
            WaitForApproval = waitForApproval,
            Source = SourceMetadataMapper.Map(source)
        };

        if (force)
        {
            input = input with { Force = true };
        }

        var result = await apiClient.PublishMcpFeatureCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).PublishMcpFeatureCollection;
    }

    public async IAsyncEnumerable<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate> SubscribeToMcpFeatureCollectionPublishAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stopSignal = new ReplaySubject<Unit>(1);
        await using var _ = cancellationToken.Register(stopSignal);

        var subscription = apiClient.PublishMcpFeatureCollectionCommandSubscription
            .Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        // The cancellation token is intentionally not passed to ToAsyncEnumerable() to avoid
        // an OperationCanceledException. Cancellation is handled via the stop signal above,
        // which completes the sequence cleanly.
        await foreach (var update in subscription.ToAsyncEnumerable())
        {
            var data = OperationResultHelper.EnsureData(update);

            yield return data.OnMcpFeatureCollectionVersionPublishingUpdate;
        }
    }
}
