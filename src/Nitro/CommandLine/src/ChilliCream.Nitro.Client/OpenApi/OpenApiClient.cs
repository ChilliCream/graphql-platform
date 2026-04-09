using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using StrawberryShake;

namespace ChilliCream.Nitro.Client.OpenApi;

internal sealed class OpenApiClient(IApiClient apiClient) : IOpenApiClient
{
    public async Task<ICreateOpenApiCollectionCommandMutation_CreateOpenApiCollection> CreateOpenApiCollectionAsync(
        string apiId,
        string name,
        CancellationToken cancellationToken)
    {
        var input = new CreateOpenApiCollectionInput
        {
            ApiId = apiId,
            Name = name
        };

        var result = await apiClient.CreateOpenApiCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        return data.CreateOpenApiCollection;
    }

    public async Task<IDeleteOpenApiCollectionByIdCommandMutation_DeleteOpenApiCollectionById> DeleteOpenApiCollectionAsync(
        string openApiCollectionId,
        CancellationToken cancellationToken)
    {
        var input = new DeleteOpenApiCollectionByIdInput
        {
            OpenApiCollectionId = openApiCollectionId
        };

        var result = await apiClient.DeleteOpenApiCollectionByIdCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).DeleteOpenApiCollectionById;
    }

    public async Task<ConnectionPage<IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node>?> ListOpenApiCollectionsAsync(
        string apiId,
        string? after,
        int? first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.ListOpenApiCollectionCommandQuery.ExecuteAsync(
            apiId,
            after,
            first,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        var connection = (data.Node as IListOpenApiCollectionCommandQuery_Node_Api)?.OpenApiCollections;
        if (connection is null)
        {
            return null;
        }

        var items = connection.Edges?.Select(static edge => edge.Node).ToArray() ?? [];

        return new ConnectionPage<IListOpenApiCollectionCommandQuery_Node_OpenApiCollections_Edges_Node>(
            items,
            connection.PageInfo.EndCursor,
            connection.PageInfo.HasNextPage);
    }

    public async Task<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection> UploadOpenApiCollectionVersionAsync(
        string openApiCollectionId,
        string tag,
        Stream collectionArchiveStream,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new UploadOpenApiCollectionInput
        {
            OpenApiCollectionId = openApiCollectionId,
            Tag = tag,
            Collection = new Upload(collectionArchiveStream, "collection.zip"),
            Source = SourceMetadataMapper.Map(source)
        };

        var result = await apiClient.UploadOpenApiCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).UploadOpenApiCollection;
    }

    public async Task<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection> StartOpenApiCollectionValidationAsync(
        string openApiCollectionId,
        string stageName,
        Stream collectionArchiveStream,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new ValidateOpenApiCollectionInput
        {
            OpenApiCollectionId = openApiCollectionId,
            Stage = stageName,
            Collection = new Upload(collectionArchiveStream, "collection.zip"),
            Source = SourceMetadataMapper.Map(source)
        };

        var result = await apiClient.ValidateOpenApiCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).ValidateOpenApiCollection;
    }

    public async IAsyncEnumerable<IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate> SubscribeToOpenApiCollectionValidationAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stopSignal = new ReplaySubject<Unit>(1);
        await using var _ = cancellationToken.Register(stopSignal);

        var subscription = apiClient.ValidateOpenApiCollectionCommandSubscription
            .Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        // The cancellation token is intentionally not passed to ToAsyncEnumerable() to avoid
        // an OperationCanceledException. Cancellation is handled via the stop signal above,
        // which completes the sequence cleanly.
        await foreach (var update in subscription.ToAsyncEnumerable())
        {
            var data = OperationResultHelper.EnsureData(update);

            yield return data.OnOpenApiCollectionVersionValidationUpdate;
        }
    }

    public async Task<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection> StartOpenApiCollectionPublishAsync(
        string openApiCollectionId,
        string stageName,
        string tag,
        bool force,
        bool waitForApproval,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new PublishOpenApiCollectionInput
        {
            OpenApiCollectionId = openApiCollectionId,
            Stage = stageName,
            Tag = tag,
            WaitForApproval = waitForApproval,
            Source = SourceMetadataMapper.Map(source)
        };

        if (force)
        {
            input = input with { Force = true };
        }

        var result = await apiClient.PublishOpenApiCollectionCommandMutation.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).PublishOpenApiCollection;
    }

    public async IAsyncEnumerable<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate> SubscribeToOpenApiCollectionPublishAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stopSignal = new ReplaySubject<Unit>(1);
        await using var _ = cancellationToken.Register(stopSignal);

        var subscription = apiClient.PublishOpenApiCollectionCommandSubscription
            .Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        // The cancellation token is intentionally not passed to ToAsyncEnumerable() to avoid
        // an OperationCanceledException. Cancellation is handled via the stop signal above,
        // which completes the sequence cleanly.
        await foreach (var update in subscription.ToAsyncEnumerable())
        {
            var data = OperationResultHelper.EnsureData(update);

            yield return data.OnOpenApiCollectionVersionPublishingUpdate;
        }
    }
}
