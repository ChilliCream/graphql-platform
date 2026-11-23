using System.Collections.Immutable;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed partial class HttpSourceSchemaClient
{
    /// <summary>
    /// Gets whether <see cref="ExecuteBufferedAsync"/> can execute a request of the given shape.
    /// Subscriptions and file uploads cannot.
    /// </summary>
    /// <param name="operationType">The operation type of the request.</param>
    /// <param name="requiresFileUpload">Whether the request carries file uploads.</param>
    internal static bool CanExecuteBuffered(OperationType operationType, bool requiresFileUpload)
        => operationType is not OperationType.Subscription && !requiresFileUpload;

    /// <summary>
    /// Executes a request and hands its results to <paramref name="sink"/> in the order they are read.
    /// The results are the same as those of <see cref="ExecuteAsync"/>.
    /// </summary>
    internal ValueTask ExecuteBufferedAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        ISourceSchemaResultSink sink,
        CancellationToken cancellationToken)
    {
        if (_useAliasBatchingForVariableSets
            && request.Variables.Length > 1
            && CanAliasBatch(in request, out _, out _))
        {
            return ExecuteAliasBatchBufferedAsync(context, in request, sink, cancellationToken);
        }

        return ExecuteSingleBufferedAsync(context, request, sink, cancellationToken);
    }

    private async ValueTask ExecuteSingleBufferedAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        ISourceSchemaResultSink sink,
        CancellationToken cancellationToken)
    {
        var node = request.Node;
        var variables = request.Variables;
        var (httpRequest, buffer) = CreateBufferedHttpRequest(context, request);
        GraphQLHttpResponse? httpResponse = null;

        try
        {
            httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            context.TrackTransport(node, httpRequest.Uri, httpResponse.RawContentType);

            if (variables.Length <= 1)
            {
                var document = await httpResponse
                    .ReadAsResultAsync(context.Memory, cancellationToken)
                    .ConfigureAwait(false);
                OnResult(context, node, sink, requestIndex: 0, CreateSingleResult(variables, document));
                return;
            }

            var mapper = new VariableBatchResultMapper(variables, _supportsVariableBatching);

            await foreach (var document in httpResponse
                .ReadAsResultStreamAsync(context.MemorySource)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                if (!mapper.TryMap(document, out var result))
                {
                    break;
                }

                if (!OnResult(context, node, sink, requestIndex: 0, result))
                {
                    return;
                }
            }

            for (var i = 0; i < mapper.ErrorResultCount; i++)
            {
                if (!OnResult(context, node, sink, requestIndex: 0, mapper.CreateErrorResult(i)))
                {
                    return;
                }
            }
        }
        finally
        {
            httpResponse?.Dispose();
            buffer?.Dispose();
        }
    }

    /// <summary>
    /// Gets whether <see cref="ExecuteBatchBufferedAsync"/> can execute the batch. Batches that contain
    /// a subscription cannot, and neither can batches that are not merged into one aliased operation
    /// and are sent as single requests or carry file uploads.
    /// </summary>
    internal bool CanExecuteBatchBuffered(ImmutableArray<SourceSchemaClientRequest> requests)
        => requests.Length > 0
            && !ContainsSubscriptionRequest(requests)
            && (_useAliasBatchingForOperationBatches
                || (Capabilities.HasFlag(SourceSchemaClientCapabilities.RequestBatching)
                    && !requests.ItemRef(0).RequiresFileUpload));

    /// <summary>
    /// Executes a batch as one batched request and hands its results to <paramref name="sink"/>
    /// in the order they are read. The results are the same as those of <see cref="ExecuteBatchAsync"/>.
    /// </summary>
    internal ValueTask ExecuteBatchBufferedAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        ISourceSchemaResultSink sink,
        CancellationToken cancellationToken)
        => _useAliasBatchingForOperationBatches
            ? ExecuteAliasBatchBufferedAsync(context, requests, sink, cancellationToken)
            : ExecuteOperationBatchBufferedAsync(context, requests, sink, cancellationToken);

    private async ValueTask ExecuteOperationBatchBufferedAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        ISourceSchemaResultSink sink,
        CancellationToken cancellationToken)
    {
        var (httpRequest, buffer) = CreateBufferedHttpBatchRequest(context, requests);
        GraphQLHttpResponse? httpResponse = null;

        try
        {
            httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            context.TrackTransport(requests.ItemRef(0).Node, httpRequest.Uri, httpResponse.RawContentType);

            var mapper = new BatchResultMapper(requests, _supportsVariableBatching);

            await foreach (var document in httpResponse
                .ReadAsResultStreamAsync(context.MemorySource)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                mapper.Begin(document);

                while (mapper.TryGetNext(out var requestIndex, out var result))
                {
                    if (!OnResult(context, requests.ItemRef(requestIndex).Node, sink, requestIndex, result))
                    {
                        return;
                    }
                }
            }
        }
        finally
        {
            httpResponse?.Dispose();
            buffer?.Dispose();
        }
    }

    private (GraphQLHttpRequest HttpRequest, ChunkedArrayWriter? Buffer) CreateBufferedHttpRequest(
        OperationPlanContext context,
        SourceSchemaClientRequest request)
    {
        ChunkedArrayWriter? buffer = null;

        try
        {
            var httpRequest = CreateHttpRequest(context, in request, ref buffer);
            SourceSchemaCallbackHelper.ConfigureCallbacks(httpRequest, context, request.Node, _configuration);
            return (httpRequest, buffer);
        }
        catch
        {
            buffer?.Dispose();
            throw;
        }
    }

    private (GraphQLHttpRequest HttpRequest, ChunkedArrayWriter? Buffer) CreateBufferedHttpBatchRequest(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests)
    {
        ChunkedArrayWriter? buffer = null;

        try
        {
            ref readonly var firstRequest = ref requests.ItemRef(0);
            var httpRequest = CreateHttpBatchRequest(context, requests, firstRequest.RequiresFileUpload, ref buffer);
            SourceSchemaCallbackHelper.ConfigureCallbacks(httpRequest, context, firstRequest.Node, _configuration);
            return (httpRequest, buffer);
        }
        catch
        {
            buffer?.Dispose();
            throw;
        }
    }

    private bool OnResult(
        OperationPlanContext context,
        ExecutionNode node,
        ISourceSchemaResultSink sink,
        int requestIndex,
        SourceSchemaResult result)
    {
        _configuration.OnSourceSchemaResult?.Invoke(context, node, result);
        return sink.OnResult(requestIndex, result);
    }
}
