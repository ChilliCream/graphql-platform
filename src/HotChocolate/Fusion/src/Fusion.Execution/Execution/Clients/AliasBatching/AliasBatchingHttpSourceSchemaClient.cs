using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Caching.Memory;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// HTTP-based implementation of <see cref="ISourceSchemaClient"/> that rewrites each row of a
/// batch into an aliased copy of the root selections within a single spec-conformant GraphQL
/// request. Unlike <see cref="HttpSourceSchemaClient"/>, this client requires no protocol
/// extension from the downstream service.
/// </summary>
internal sealed class AliasBatchingHttpSourceSchemaClient : ISourceSchemaClient
{
    private static ReadOnlySpan<byte> QueryProperty => "query"u8;
    private static ReadOnlySpan<byte> VariablesProperty => "variables"u8;

    private readonly GraphQLHttpClient _client;
    private readonly HttpSourceSchemaClientConfiguration _configuration;
    private readonly AliasBatchingRewriter _rewriter = new();
    private readonly Cache<AliasBatchedOperation> _cache;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="AliasBatchingHttpSourceSchemaClient"/>.
    /// </summary>
    /// <param name="client">The underlying HTTP client used to send requests.</param>
    /// <param name="configuration">The transport configuration for this source schema.</param>
    public AliasBatchingHttpSourceSchemaClient(
        GraphQLHttpClient client,
        HttpSourceSchemaClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configuration);

        _client = client;
        _configuration = configuration;
        _cache = new Cache<AliasBatchedOperation>(configuration.AliasBatchingCacheCapacity);
    }

    /// <inheritdoc />
    public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.AliasBatching;

    /// <inheritdoc />
    public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        EnsureSupported(request);

        // A request with zero or one variable row carries no batch to merge, so it is sent as a
        // plain single GraphQL request with the original document and variables. This mirrors the
        // existing client, which also skips its batch envelope for a single row.
        if (request.Variables.Length <= 1)
        {
            await foreach (var result in ExecuteSingleAsync(context, request, cancellationToken)
                .ConfigureAwait(false))
            {
                yield return result;
            }

            yield break;
        }

        var requests = ImmutableArray.Create(request);
        var batched = GetOrCreateOperation(requests);
        var httpResponse = await SendBatchedAsync(context, requests, batched, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await foreach (var rowResult in AliasResponseReader
                .ReadAsync(httpResponse, context.Memory, requests, batched, cancellationToken)
                .ConfigureAwait(false))
            {
                _configuration.OnSourceSchemaResult?.Invoke(context, request.Node, rowResult.Result);

                yield return rowResult.Result;
            }
        }
        finally
        {
            httpResponse.Dispose();
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfLessThan(requests.Length, 1, nameof(requests));

        foreach (var request in requests)
        {
            EnsureSupported(request);
        }

        // A single request with at most one row carries no batch to merge, so it is sent through
        // the same plain single GraphQL request path as ExecuteAsync.
        if (requests.Length == 1 && requests[0].Variables.Length <= 1)
        {
            return ExecuteSingleStreamAsync(context, requests[0], cancellationToken);
        }

        var batched = GetOrCreateOperation(requests);

        return ExecuteBatchStreamCoreAsync(context, requests, batched, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "Alias batching is not supported for subscriptions.");
    }

    private async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchStreamCoreAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        AliasBatchedOperation batched,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var httpResponse = await SendBatchedAsync(context, requests, batched, cancellationToken)
            .ConfigureAwait(false);
        var onSourceSchemaResult = _configuration.OnSourceSchemaResult;

        try
        {
            await foreach (var rowResult in AliasResponseReader
                .ReadAsync(httpResponse, context.Memory, requests, batched, cancellationToken)
                .ConfigureAwait(false))
            {
                onSourceSchemaResult?.Invoke(
                    context,
                    requests[rowResult.RequestIndex].Node,
                    rowResult.Result);

                yield return new SourceSchemaBatchResult(rowResult.RequestIndex, rowResult.Result);
            }
        }
        finally
        {
            httpResponse.Dispose();
        }
    }

    private async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteSingleStreamAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var result in ExecuteSingleAsync(context, request, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return new SourceSchemaBatchResult(0, result);
        }
    }

    private async IAsyncEnumerable<SourceSchemaResult> ExecuteSingleAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var variables = request.Variables.IsDefaultOrEmpty
            ? VariableValues.Empty
            : request.Variables[0];

        var body = new OperationRequest(
            request.OperationSourceText,
            id: null,
            operationName: null,
            onError: _configuration.OnError,
            variables: variables,
            extensions: JsonSegment.Empty);

        var httpRequest = new GraphQLHttpRequest(body)
        {
            Uri = _configuration.BaseAddress,
            AcceptHeaderValue = _configuration.DefaultAcceptHeaderValue,
            OperationKind = request.OperationType
        };

        SourceSchemaCallbackHelper.ConfigureCallbacks(httpRequest, context, request.Node, _configuration);

        GraphQLHttpResponse? httpResponse = null;

        try
        {
            httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            context.TrackTransport(request.Node, httpRequest.Uri, httpResponse.RawContentType);

            var result = await httpResponse.ReadAsResultAsync(context.Memory, cancellationToken)
                .ConfigureAwait(false);

            var sourceSchemaResult = variables.IsEmpty
                ? new SourceSchemaResult(CompactPath.Root, result)
                : new SourceSchemaResult(
                    variables.Path,
                    result,
                    additionalPaths: variables.AdditionalPaths);

            _configuration.OnSourceSchemaResult?.Invoke(context, request.Node, sourceSchemaResult);

            yield return sourceSchemaResult;
        }
        finally
        {
            httpResponse?.Dispose();
        }
    }

    private async ValueTask<GraphQLHttpResponse> SendBatchedAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        AliasBatchedOperation batched,
        CancellationToken cancellationToken)
    {
        // The merged body is built once into a pooled buffer so the merged variables can be
        // assembled with a single writer. The buffer is held until the transport has serialized
        // the request, then released.
        var bodyWriter = new PooledArrayWriter();

        try
        {
            await using (var json = new Utf8JsonWriter(bodyWriter))
            {
                json.WriteStartObject();
                json.WriteString(QueryProperty, batched.SourceText);
                json.WritePropertyName(VariablesProperty);
                AliasVariableMerger.Write(json, batched.Prefixes, requests);
                json.WriteEndObject();
            }

            var httpRequest = new GraphQLHttpRequest(new AliasBatchedRequestBody(bodyWriter.WrittenMemory))
            {
                Uri = _configuration.BaseAddress,
                // Alias batching sends one standard GraphQL request whose response is a single
                // graphql-response+json document, so the default accept header applies, not the
                // batching one used by the protocol-extension batch modes.
                AcceptHeaderValue = _configuration.DefaultAcceptHeaderValue,
                OperationKind = requests[0].OperationType
            };

            SourceSchemaCallbackHelper.ConfigureCallbacks(
                httpRequest,
                context,
                requests[0].Node,
                _configuration);

            var httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            context.TrackTransport(requests[0].Node, httpRequest.Uri, httpResponse.RawContentType);

            return httpResponse;
        }
        finally
        {
            bodyWriter.Dispose();
        }
    }

    /// <summary>
    /// Gets the alias batched operation for the given requests, rewriting and caching it on a miss.
    /// </summary>
    /// <param name="requests">The inbound requests to merge into a single alias batched operation.</param>
    /// <returns>The cached or freshly rewritten alias batched operation.</returns>
    internal AliasBatchedOperation GetOrCreateOperation(
        ImmutableArray<SourceSchemaClientRequest> requests)
    {
        var maxKeyLength = AliasBatchCacheKey.GetMaxKeyLength(requests);

#if NET9_0_OR_GREATER
        char[]? rentedKey = null;
        var keyBuffer = maxKeyLength <= 192
            ? stackalloc char[192]
            : (rentedKey = System.Buffers.ArrayPool<char>.Shared.Rent(maxKeyLength));

        try
        {
            var keyLength = AliasBatchCacheKey.Build(keyBuffer, requests);
            var key = keyBuffer[..keyLength];

            return _cache.GetOrCreate(
                key,
                static (_, state) => state.rewriter.Rewrite(state.requests),
                (rewriter: _rewriter, requests));
        }
        finally
        {
            if (rentedKey is not null)
            {
                System.Buffers.ArrayPool<char>.Shared.Return(rentedKey);
            }
        }
#else
        var keyBuffer = maxKeyLength <= 192
            ? stackalloc char[192]
            : new char[maxKeyLength];

        var keyLength = AliasBatchCacheKey.Build(keyBuffer, requests);
        var key = new string(keyBuffer[..keyLength]);

        return _cache.GetOrCreate(
            key,
            static (_, state) => state.rewriter.Rewrite(state.requests),
            (rewriter: _rewriter, requests));
#endif
    }

    private static void EnsureSupported(SourceSchemaClientRequest request)
    {
        if (request.OperationType is OperationType.Subscription)
        {
            throw new NotSupportedException(
                "Alias batching is not supported for subscriptions.");
        }

        if (request.RequiresFileUpload)
        {
            throw new NotSupportedException(
                "Alias batching is incompatible with file uploads; "
                + "disable AliasBatching for this subgraph.");
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _client.Dispose();
        _disposed = true;

        return ValueTask.CompletedTask;
    }
}
