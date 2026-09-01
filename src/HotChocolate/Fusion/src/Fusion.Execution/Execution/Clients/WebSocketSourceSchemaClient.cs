using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using HotChocolate.Fusion.Properties;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Sockets;
using HotChocolate.Fusion.Transport.Sockets.Client;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Executes source schema operations over a shared request-scoped WebSocket connection.
/// </summary>
public sealed class WebSocketSourceSchemaClient : ISourceSchemaClient
{
    private static ReadOnlySpan<byte> VariableIndex => "variableIndex"u8;
    private static ReadOnlySpan<byte> RequestIndex => "requestIndex"u8;

    private readonly ConcurrentDictionary<int, ActiveOperation> _activeOperations = [];
    private readonly SemaphoreSlim _connectGate = new(1, 1);
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly HttpMessageInvoker _invoker;
    private readonly WebSocketSourceSchemaClientConfiguration _configuration;
    private readonly WebSocketConnector _connect;
    private SocketClient? _socketClient;
    private ExceptionDispatchInfo? _connectError;
    private int _nextOperationId;
    private int _disposed;

    /// <summary>
    /// Initializes a source schema WebSocket client.
    /// </summary>
    /// <param name="invoker">The shared HTTP message invoker used to establish the connection.</param>
    /// <param name="configuration">The source schema WebSocket configuration.</param>
    public WebSocketSourceSchemaClient(
        HttpMessageInvoker invoker,
        WebSocketSourceSchemaClientConfiguration configuration)
        : this(invoker, configuration, ConnectWebSocketAsync)
    {
    }

    internal WebSocketSourceSchemaClient(
        HttpMessageInvoker invoker,
        WebSocketSourceSchemaClientConfiguration configuration,
        WebSocketConnector connect)
    {
        ArgumentNullException.ThrowIfNull(invoker);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(connect);

        _invoker = invoker;
        _configuration = configuration;
        _connect = connect;
        Capabilities = configuration.Capabilities;
    }

    /// <inheritdoc />
    public SourceSchemaClientCapabilities Capabilities { get; }

    /// <inheritdoc />
    public IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (request.OperationType is OperationType.Subscription)
        {
            throw new InvalidOperationException(
                FusionExecutionResources.SourceSchemaClient_SubscriptionsNotSupportedByExecute);
        }

        return ExecuteInternalAsync(context, request, subscribe: false, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfLessThan(requests.Length, 1, nameof(requests));

        if (ContainsSubscriptionRequest(requests))
        {
            throw new InvalidOperationException(
                FusionExecutionResources.HttpSourceSchemaClient_SubscriptionBatchNotSupported);
        }

        return Capabilities.HasFlag(SourceSchemaClientCapabilities.RequestBatching)
            ? ExecuteBatchInternalAsync(context, requests, cancellationToken)
            : ExecuteBatchAsSingleRequestsAsync(context, requests, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ExecuteInternalAsync(context, request, subscribe: true, cancellationToken);
    }

    private async IAsyncEnumerable<SourceSchemaResult> ExecuteInternalAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        bool subscribe,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        SocketResult socketResult;
        ActiveOperation activeOperation;

        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            var client = await GetSocketClientAsync(cancellationToken).ConfigureAwait(false);

            if (!subscribe
                && request.Variables.Length > 1
                && !Capabilities.HasFlag(SourceSchemaClientCapabilities.VariableBatching))
            {
                socketResult = await client.ExecuteBatchAsync(
                    CreateVariableOperationBatch(request),
                    context.MemorySource,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var operationRequest = CreateOperationRequest(request);
                socketResult = subscribe
                    ? await client.SubscribeAsync(
                        operationRequest,
                        context.MemorySource,
                        cancellationToken).ConfigureAwait(false)
                    : await client.ExecuteAsync(
                        operationRequest,
                        context.MemorySource,
                        cancellationToken).ConfigureAwait(false);
            }

            activeOperation = Track(socketResult);
        }
        finally
        {
            _operationGate.Release();
        }

        context.TrackTransport(request.Node, _configuration.Url, WellKnownProtocols.GraphQL_Transport_WS);

        try
        {
            var sequentialVariableIndex = 0;

            await foreach (var document in socketResult
                .ReadResultsAsync()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                if (request.Variables.Length <= 1)
                {
                    yield return CreateResult(request, variableIndex: 0, document);
                    continue;
                }

                var variableIndex = Capabilities.HasFlag(SourceSchemaClientCapabilities.VariableBatching)
                    ? ResolveVariableIndex(request, document)
                    : ResolveSequentialVariableIndex(request, document, ref sequentialVariableIndex);

                if (variableIndex >= 0)
                {
                    yield return CreateResult(request, variableIndex, document);
                }
                else
                {
                    foreach (var result in CreateSharedResults(request, document))
                    {
                        yield return result;
                    }
                }
            }
        }
        finally
        {
            await activeOperation.CompleteAsync().ConfigureAwait(false);
            _activeOperations.TryRemove(activeOperation.Id, out _);
        }
    }

    private async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchInternalAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        SocketResult socketResult;
        ActiveOperation activeOperation;
        ImmutableArray<BatchEntry> entries;

        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            var client = await GetSocketClientAsync(cancellationToken).ConfigureAwait(false);
            var batch = CreateOperationBatch(requests);
            entries = batch.Entries;
            socketResult = await client.ExecuteBatchAsync(
                batch.Request,
                context.MemorySource,
                cancellationToken).ConfigureAwait(false);
            activeOperation = Track(socketResult);
        }
        finally
        {
            _operationGate.Release();
        }

        context.TrackTransport(requests[0].Node, _configuration.Url, WellKnownProtocols.GraphQL_Transport_WS);

        try
        {
            var sequentialRequestIndex = 0;

            await foreach (var document in socketResult
                .ReadResultsAsync()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                var entryIndex = ResolveRequestIndex(entries.Length, document, ref sequentialRequestIndex);

                if (entryIndex >= 0)
                {
                    var entry = entries[entryIndex];
                    var request = requests[entry.RequestIndex];
                    var variableIndex = entry.VariableIndex;

                    if (variableIndex < 0)
                    {
                        variableIndex = ResolveVariableIndex(request, document);
                    }

                    if (variableIndex >= 0)
                    {
                        yield return new SourceSchemaBatchResult(
                            entry.RequestIndex,
                            CreateResult(request, variableIndex, document));
                    }
                    else
                    {
                        foreach (var result in CreateSharedResults(request, document))
                        {
                            yield return new SourceSchemaBatchResult(entry.RequestIndex, result);
                        }
                    }
                }
                else
                {
                    foreach (var result in CreateSharedBatchResults(requests, document))
                    {
                        yield return result;
                    }
                }
            }
        }
        finally
        {
            await activeOperation.CompleteAsync().ConfigureAwait(false);
            _activeOperations.TryRemove(activeOperation.Id, out _);
        }
    }

    private async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsSingleRequestsAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < requests.Length; i++)
        {
            var enumerator = ExecuteAsync(context, requests[i], cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false)
                .GetAsyncEnumerator();

            try
            {
                while (true)
                {
                    try
                    {
                        if (!await enumerator.MoveNextAsync())
                        {
                            break;
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        context.TrackBatchRequestError(requests[i].Node, i, exception);
                        break;
                    }

                    yield return new SourceSchemaBatchResult(i, enumerator.Current);
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }
    }

    private async ValueTask<SocketClient> GetSocketClientAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        if (_socketClient is { } socketClient)
        {
            return socketClient;
        }

        _connectError?.Throw();
        await _connectGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

            if (_socketClient is not null)
            {
                return _socketClient;
            }

            _connectError?.Throw();

            try
            {
                var socket = await _connect(
                    _configuration.Url,
                    _invoker,
                    _configuration.KeepAliveInterval,
                    cancellationToken).ConfigureAwait(false);
                var options = new SocketClientOptions
                {
                    MaxOperationQueueBytes = _configuration.MaxOperationQueueBytes
                };
                var client = await SocketClient.ConnectAsync(
                    socket,
                    options,
                    cancellationToken).ConfigureAwait(false);

                _socketClient = client;
                return client;
            }
            catch (Exception exception)
            {
                _connectError = ExceptionDispatchInfo.Capture(exception);
                throw;
            }
        }
        finally
        {
            _connectGate.Release();
        }
    }

    private ActiveOperation Track(SocketResult result)
    {
        var id = Interlocked.Increment(ref _nextOperationId);
        var activeOperation = new ActiveOperation(id, result);
        _activeOperations.TryAdd(id, activeOperation);

        if (Volatile.Read(ref _disposed) != 0)
        {
            _activeOperations.TryRemove(id, out _);
            _ = activeOperation.CompleteAsync();
            ObjectDisposedException.ThrowIf(true, this);
        }

        return activeOperation;
    }

    private IOperationRequest CreateOperationRequest(SourceSchemaClientRequest request)
    {
        if (request.Variables.Length > 1
            && Capabilities.HasFlag(SourceSchemaClientCapabilities.VariableBatching))
        {
            return new VariableBatchRequest(
                request.OperationSourceText.Value,
                id: null,
                operationName: null,
                onError: null,
                request.Variables,
                JsonSegment.Empty);
        }

        return new OperationRequest(
            request.OperationSourceText.Value,
            id: null,
            operationName: null,
            onError: null,
            request.Variables.IsDefaultOrEmpty ? VariableValues.Empty : request.Variables[0],
            JsonSegment.Empty);
    }

    private static OperationBatchRequest CreateVariableOperationBatch(
        SourceSchemaClientRequest request)
    {
        var requests = ImmutableArray.CreateBuilder<IOperationRequest>(request.Variables.Length);

        foreach (var variables in request.Variables)
        {
            requests.Add(new OperationRequest(
                request.OperationSourceText.Value,
                id: null,
                operationName: null,
                onError: null,
                variables,
                JsonSegment.Empty));
        }

        return new OperationBatchRequest(requests.MoveToImmutable());
    }

    private (OperationBatchRequest Request, ImmutableArray<BatchEntry> Entries) CreateOperationBatch(
        ImmutableArray<SourceSchemaClientRequest> sourceRequests)
    {
        var requests = ImmutableArray.CreateBuilder<IOperationRequest>();
        var entries = ImmutableArray.CreateBuilder<BatchEntry>();
        var supportsVariableBatching =
            Capabilities.HasFlag(SourceSchemaClientCapabilities.VariableBatching);

        for (var requestIndex = 0; requestIndex < sourceRequests.Length; requestIndex++)
        {
            var sourceRequest = sourceRequests[requestIndex];

            if (sourceRequest.Variables.Length > 1 && supportsVariableBatching)
            {
                requests.Add(CreateOperationRequest(sourceRequest));
                entries.Add(new BatchEntry(requestIndex, VariableIndex: -1));
                continue;
            }

            if (sourceRequest.Variables.Length > 1)
            {
                for (var variableIndex = 0; variableIndex < sourceRequest.Variables.Length; variableIndex++)
                {
                    requests.Add(new OperationRequest(
                        sourceRequest.OperationSourceText.Value,
                        id: null,
                        operationName: null,
                        onError: null,
                        sourceRequest.Variables[variableIndex],
                        JsonSegment.Empty));
                    entries.Add(new BatchEntry(requestIndex, variableIndex));
                }
            }
            else
            {
                requests.Add(CreateOperationRequest(sourceRequest));
                entries.Add(new BatchEntry(requestIndex, VariableIndex: 0));
            }
        }

        return (new OperationBatchRequest(requests.MoveToImmutable()), entries.MoveToImmutable());
    }

    private static SourceSchemaResult CreateResult(
        SourceSchemaClientRequest request,
        int variableIndex,
        SourceResultDocument document)
    {
        if (request.Variables.IsDefaultOrEmpty)
        {
            return new SourceSchemaResult(CompactPath.Root, document);
        }

        var variable = request.Variables[variableIndex];
        return new SourceSchemaResult(
            variable.Path,
            document,
            additionalPaths: variable.AdditionalPaths);
    }

    private static IEnumerable<SourceSchemaResult> CreateSharedResults(
        SourceSchemaClientRequest request,
        SourceResultDocument document)
    {
        if (request.Variables.IsDefaultOrEmpty)
        {
            yield return new SourceSchemaResult(CompactPath.Root, document);
            yield break;
        }

        var owner = new SourceResultDocumentOwner(document, request.Variables.Length);

        foreach (var variable in request.Variables)
        {
            yield return new SourceSchemaResult(
                variable.Path,
                document,
                owner,
                additionalPaths: variable.AdditionalPaths);
        }
    }

    private static IEnumerable<SourceSchemaBatchResult> CreateSharedBatchResults(
        ImmutableArray<SourceSchemaClientRequest> requests,
        SourceResultDocument document)
    {
        var references = 0;

        foreach (var request in requests)
        {
            references += Math.Max(1, request.Variables.Length);
        }

        var owner = new SourceResultDocumentOwner(document, references);

        for (var requestIndex = 0; requestIndex < requests.Length; requestIndex++)
        {
            var request = requests[requestIndex];

            if (request.Variables.IsDefaultOrEmpty)
            {
                yield return new SourceSchemaBatchResult(
                    requestIndex,
                    new SourceSchemaResult(CompactPath.Root, document, owner));
                continue;
            }

            foreach (var variable in request.Variables)
            {
                yield return new SourceSchemaBatchResult(
                    requestIndex,
                    new SourceSchemaResult(
                        variable.Path,
                        document,
                        owner,
                        additionalPaths: variable.AdditionalPaths));
            }
        }
    }

    private static int ResolveVariableIndex(
        SourceSchemaClientRequest request,
        SourceResultDocument document)
    {
        if (request.Variables.Length <= 1)
        {
            return 0;
        }

        if (!document.Root.TryGetProperty(VariableIndex, out var variableIndex)
            || variableIndex.ValueKind is not JsonValueKind.Number)
        {
            return -1;
        }

        var index = variableIndex.GetInt32();

        if ((uint)index >= (uint)request.Variables.Length)
        {
            throw ThrowHelper.VariableIndexOutOfRange(index);
        }

        return index;
    }

    private static int ResolveSequentialVariableIndex(
        SourceSchemaClientRequest request,
        SourceResultDocument document,
        ref int sequentialVariableIndex)
    {
        if (document.Root.TryGetProperty(RequestIndex, out var requestIndex)
            && requestIndex.ValueKind is JsonValueKind.Number)
        {
            var index = requestIndex.GetInt32();

            if ((uint)index >= (uint)request.Variables.Length)
            {
                throw ThrowHelper.RequestIndexOutOfRange(index);
            }

            return index;
        }

        return sequentialVariableIndex < request.Variables.Length
            ? sequentialVariableIndex++
            : -1;
    }

    private static int ResolveRequestIndex(
        int requestCount,
        SourceResultDocument document,
        ref int sequentialRequestIndex)
    {
        if (document.Root.TryGetProperty(RequestIndex, out var requestIndex)
            && requestIndex.ValueKind is JsonValueKind.Number)
        {
            var index = requestIndex.GetInt32();

            if ((uint)index >= (uint)requestCount)
            {
                throw ThrowHelper.RequestIndexOutOfRange(index);
            }

            return index;
        }

        return requestCount == 1
            ? 0
            : sequentialRequestIndex < requestCount
                ? sequentialRequestIndex++
                : -1;
    }

    private static bool ContainsSubscriptionRequest(
        ImmutableArray<SourceSchemaClientRequest> requests)
    {
        foreach (var request in requests)
        {
            if (request.OperationType is OperationType.Subscription)
            {
                return true;
            }
        }

        return false;
    }

    private static async ValueTask<WebSocket> ConnectWebSocketAsync(
        Uri url,
        HttpMessageInvoker invoker,
        TimeSpan keepAliveInterval,
        CancellationToken cancellationToken)
    {
        var socket = new ClientWebSocket();
        socket.Options.AddSubProtocol(WellKnownProtocols.GraphQL_Transport_WS);
        socket.Options.KeepAliveInterval = keepAliveInterval;
        socket.Options.HttpVersion = HttpVersion.Version20;
        socket.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

        try
        {
            await socket.ConnectAsync(url, invoker, cancellationToken).ConfigureAwait(false);
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await _operationGate.WaitAsync().ConfigureAwait(false);

        try
        {
            var operations = _activeOperations.Values.ToArray();
            await Task.WhenAll(operations.Select(static t => t.CompleteAsync())).ConfigureAwait(false);

            if (_socketClient is not null)
            {
                await _socketClient.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    null,
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    internal delegate ValueTask<WebSocket> WebSocketConnector(
        Uri url,
        HttpMessageInvoker invoker,
        TimeSpan keepAliveInterval,
        CancellationToken cancellationToken);

    private readonly record struct BatchEntry(int RequestIndex, int VariableIndex);

    private sealed class ActiveOperation(int id, SocketResult result)
    {
        private readonly object _sync = new();
        private Task? _completion;

        public int Id { get; } = id;

        public Task CompleteAsync()
        {
            lock (_sync)
            {
                return _completion ??= result.CompleteAsync(CancellationToken.None).AsTask();
            }
        }
    }
}
