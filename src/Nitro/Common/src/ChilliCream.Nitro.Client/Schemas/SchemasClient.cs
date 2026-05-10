using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using StrawberryShake;

namespace ChilliCream.Nitro.Client.Schemas;

internal sealed class SchemasClient(
    IApiClient apiClient,
    IHttpClientFactory httpClientFactory)
    : ISchemasClient
{
    public async Task<IUploadSchema_UploadSchema> UploadSchemaAsync(
        string apiId,
        string tag,
        Stream schemaStream,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new UploadSchemaInput
        {
            ApiId = apiId,
            Tag = tag,
            Schema = new Upload(schemaStream, "schema.graphql"),
            Source = SourceMetadataMapper.Map(source)
        };

        var result = await apiClient.UploadSchema.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).UploadSchema;
    }

    public async Task<IValidateSchemaVersion_ValidateSchema> StartSchemaValidationAsync(
        string apiId,
        string stageName,
        Stream schemaStream,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new ValidateSchemaInput
        {
            ApiId = apiId,
            Stage = stageName,
            Schema = new Upload(schemaStream, "schema.graphql"),
            Source = SourceMetadataMapper.Map(source)
        };

        var result = await apiClient.ValidateSchemaVersion.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).ValidateSchema;
    }

    public async IAsyncEnumerable<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate> SubscribeToSchemaValidationAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stopSignal = new ReplaySubject<Unit>(1);
        await using var _ = cancellationToken.Register(stopSignal);

        var subscription = apiClient.OnSchemaVersionValidationUpdated
            .Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        // The cancellation token is intentionally not passed to ToAsyncEnumerable() to avoid
        // an OperationCanceledException. Cancellation is handled via the stop signal above,
        // which completes the sequence cleanly.
        await foreach (var update in subscription.ToAsyncEnumerable())
        {
            var data = OperationResultHelper.EnsureData(update);

            yield return data.OnSchemaVersionValidationUpdate;
        }
    }

    public async Task<IPublishSchemaVersion_PublishSchema> StartSchemaPublishAsync(
        string apiId,
        string stageName,
        string tag,
        bool force,
        bool waitForApproval,
        SourceMetadata? source,
        CancellationToken cancellationToken)
    {
        var input = new PublishSchemaInput
        {
            ApiId = apiId,
            Stage = stageName,
            Tag = tag,
            WaitForApproval = waitForApproval,
            Source = SourceMetadataMapper.Map(source)
        };

        if (force)
        {
            input = input with { Force = true };
        }

        var result = await apiClient.PublishSchemaVersion.ExecuteAsync(input, cancellationToken);

        return OperationResultHelper.EnsureData(result).PublishSchema;
    }

    public async IAsyncEnumerable<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate> SubscribeToSchemaPublishAsync(
        string requestId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stopSignal = new ReplaySubject<Unit>(1);
        await using var _ = cancellationToken.Register(stopSignal);

        var subscription = apiClient.OnSchemaVersionPublishUpdated
            .Watch(requestId, ExecutionStrategy.NetworkOnly)
            .TakeUntil(stopSignal);

        // The cancellation token is intentionally not passed to ToAsyncEnumerable() to avoid
        // an OperationCanceledException. Cancellation is handled via the stop signal above,
        // which completes the sequence cleanly.
        await foreach (var update in subscription.ToAsyncEnumerable())
        {
            var data = OperationResultHelper.EnsureData(update);

            yield return data.OnSchemaVersionPublishingUpdate;
        }
    }

    public async Task<Stream?> DownloadLatestSchemaAsync(
        string apiId,
        string stageName,
        CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(ApiClient.ClientName);

        var encodedApiId = Uri.EscapeDataString(apiId);
        var encodedStageName = Uri.EscapeDataString(stageName);

        using var response = await httpClient.GetAsync(
            $"/api/v1/apis/{encodedApiId}/schemas/latest/download?stage={encodedStageName}",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new NitroClientAuthorizationException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new NitroClientHttpRequestException(response.StatusCode);
        }

        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }
}
