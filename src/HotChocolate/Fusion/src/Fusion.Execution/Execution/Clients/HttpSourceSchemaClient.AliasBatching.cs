using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Execution.Clients.AliasBatching;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed partial class HttpSourceSchemaClient
{
    private static ReadOnlySpan<byte> ErrorsProperty => "errors"u8;

    /// <summary>
    /// Sends the variable sets of one request as a single batched request and streams the result
    /// of every set back in request order.
    /// </summary>
    private async IAsyncEnumerable<SourceSchemaResult> ExecuteAliasBatchAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requests = ImmutableArray.Create(request);
        var items = new AliasBatchItem[request.Variables.Length];
        var sharedVariables = new List<Utf8VariableDefinitionNode>();
        var itemCount = CollectItems(requests.AsSpan(), items, sharedVariables);

        await foreach (var result in ExecuteAliasBatchStreamAsync(
            context, requests, items, itemCount, sharedVariables, cancellationToken)
            .WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return result.Result;
        }
    }

    /// <summary>
    /// Sends every batchable request of the batch as a single batched request and streams the
    /// results of the requests that peel off individually alongside it.
    /// </summary>
    private async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteAliasBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var items = new AliasBatchItem[CountVariableSets(requests)];
        var sharedVariables = new List<Utf8VariableDefinitionNode>();
        var itemCount = CollectItems(requests.AsSpan(), items, sharedVariables);

        // A batch that merges fewer than two items gains nothing from the alias protocol, so
        // every request stays on the protocol the source schema natively supports.
        if (itemCount < 2)
        {
            await foreach (var result in ExecuteBatchWithNativeProtocolAsync(
                context, requests, cancellationToken)
                .WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return result;
            }

            yield break;
        }

        await foreach (var result in ExecuteAliasBatchStreamAsync(
            context, requests, items, itemCount, sharedVariables, cancellationToken)
            .WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return result;
        }

        // A request that cannot join the batch, for example because it uploads files, is sent on
        // its own so that the remaining requests still share one round-trip.
        for (var i = 0; i < requests.Length; i++)
        {
            if (CanAliasBatch(requests[i], out _, out _))
            {
                continue;
            }

            await foreach (var result in ExecuteIsolatedAsync(context, requests[i], i, cancellationToken)
                .WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Sends the variable sets of one request as a single batched request and hands the result of
    /// every set to <paramref name="sink"/> in request order.
    /// </summary>
    private ValueTask ExecuteAliasBatchBufferedAsync(
        OperationPlanContext context,
        in SourceSchemaClientRequest request,
        ISourceSchemaResultSink sink,
        CancellationToken cancellationToken)
    {
        var itemBufferLength = request.Variables.Length;
        var items = ArrayPool<AliasBatchItem>.Shared.Rent(itemBufferLength);
        GraphQLHttpRequest httpRequest;
        int itemCount;

        try
        {
            var requests = new ReadOnlySpan<SourceSchemaClientRequest>(in request);
            var sharedVariables = new List<Utf8VariableDefinitionNode>();
            itemCount = CollectItems(requests, items, sharedVariables);
            httpRequest = CreateAliasBatchHttpRequest(context, requests, items, itemCount, sharedVariables);
        }
        catch
        {
            ReturnItems(items, itemBufferLength);
            throw;
        }

        return SendAliasBatchAsync(
            context,
            request.Node,
            request.SchemaName,
            httpRequest,
            items,
            itemCount,
            sink,
            cancellationToken);
    }

    /// <summary>
    /// Sends every request of the batch as a single batched request and hands the results to
    /// <paramref name="sink"/> in request order.
    /// </summary>
    private ValueTask ExecuteAliasBatchBufferedAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        ISourceSchemaResultSink sink,
        CancellationToken cancellationToken)
    {
        var itemBufferLength = CountVariableSets(requests);
        var items = ArrayPool<AliasBatchItem>.Shared.Rent(itemBufferLength);
        GraphQLHttpRequest httpRequest;
        int itemCount;

        try
        {
            var sharedVariables = new List<Utf8VariableDefinitionNode>();
            itemCount = CollectItems(requests.AsSpan(), items, sharedVariables);

            // A batch with too few items or with a request that cannot join it is executed on the
            // streaming path, which sends those requests on their own.
            if (itemCount < 2 || itemCount < itemBufferLength)
            {
                ReturnItems(items, itemBufferLength);
                return ForwardToSinkAsync(
                    ExecuteAliasBatchAsync(context, requests, cancellationToken),
                    sink,
                    cancellationToken);
            }

            httpRequest = CreateAliasBatchHttpRequest(
                context,
                requests.AsSpan(),
                items,
                itemCount,
                sharedVariables);
        }
        catch
        {
            ReturnItems(items, itemBufferLength);
            throw;
        }

        ref readonly var firstRequest = ref requests.ItemRef(0);

        return SendAliasBatchAsync(
            context,
            firstRequest.Node,
            firstRequest.SchemaName,
            httpRequest,
            items,
            itemCount,
            sink,
            cancellationToken);
    }

    /// <summary>
    /// Sends a batched request, hands the result of every item to <paramref name="sink"/> in item
    /// order and returns <paramref name="items"/> to the pool.
    /// </summary>
    private async ValueTask SendAliasBatchAsync(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        GraphQLHttpRequest httpRequest,
        AliasBatchItem[] items,
        int itemCount,
        ISourceSchemaResultSink sink,
        CancellationToken cancellationToken)
    {
        var results = ArrayPool<SourceSchemaResult>.Shared.Rent(itemCount);
        var resultCount = 0;
        var delivered = 0;

        try
        {
            GraphQLHttpResponse? httpResponse = null;

            try
            {
                httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
                context.TrackTransport(node, httpRequest.Uri, httpResponse.RawContentType);

                var document = await httpResponse
                    .ReadAsResultAsync(context.Memory, cancellationToken)
                    .ConfigureAwait(false);

                try
                {
                    SplitResponse(document, items.AsSpan(0, itemCount), results, schemaName);
                    resultCount = itemCount;
                }
                catch
                {
                    document.Dispose();
                    throw;
                }
            }
            finally
            {
                httpResponse?.Dispose();
            }

            while (delivered < resultCount)
            {
                var result = results[delivered];
                var requestIndex = items[delivered].RequestIndex;

                // The sink frees the result it receives, so the result leaves the undelivered
                // range before it is handed out.
                delivered++;

                if (!OnResult(context, node, sink, requestIndex, result))
                {
                    break;
                }
            }
        }
        finally
        {
            // A sink that stops before the last item leaves the results it did not receive
            // behind, so their hold on the shared response document is released here.
            for (var i = delivered; i < resultCount; i++)
            {
                results[i].Dispose();
            }

            results.AsSpan(0, itemCount).Clear();
            ArrayPool<SourceSchemaResult>.Shared.Return(results);
            ReturnItems(items, itemCount);
        }
    }

    private static async ValueTask ForwardToSinkAsync(
        IAsyncEnumerable<SourceSchemaBatchResult> results,
        ISourceSchemaResultSink sink,
        CancellationToken cancellationToken)
    {
        await foreach (var result in results.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (!sink.OnResult(result.RequestIndex, result.Result))
            {
                break;
            }
        }
    }

    private static void ReturnItems(AliasBatchItem[] items, int length)
    {
        items.AsSpan(0, length).Clear();
        ArrayPool<AliasBatchItem>.Shared.Return(items);
    }

    private async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteAliasBatchStreamAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        AliasBatchItem[] items,
        int itemCount,
        List<Utf8VariableDefinitionNode> sharedVariables,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = requests[0];
        var node = request.Node;
        var httpRequest = CreateAliasBatchHttpRequest(
            context,
            requests.AsSpan(),
            items,
            itemCount,
            sharedVariables);

        GraphQLHttpResponse? httpResponse = null;
        var results = new SourceSchemaResult[itemCount];

        try
        {
            httpResponse = await _client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            context.TrackTransport(node, httpRequest.Uri, httpResponse.RawContentType);

            var document = await httpResponse
                .ReadAsResultAsync(context.Memory, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                SplitResponse(document, items.AsSpan(0, itemCount), results, request.SchemaName);
            }
            catch
            {
                document.Dispose();
                throw;
            }
        }
        finally
        {
            httpResponse?.Dispose();
        }

        var onSourceSchemaResult = _configuration.OnSourceSchemaResult;
        var pending = 0;

        try
        {
            while (pending < itemCount)
            {
                var result = results[pending];
                onSourceSchemaResult?.Invoke(context, node, result);
                var requestIndex = items[pending].RequestIndex;

                // The caller frees the result it receives, so the result leaves the pending range
                // before it is handed out.
                pending++;

                yield return new SourceSchemaBatchResult(requestIndex, result);
            }
        }
        finally
        {
            // A caller that stops before the last item leaves the results it did not receive
            // behind, so their hold on the shared response document is released here.
            for (var i = pending; i < itemCount; i++)
            {
                results[i].Dispose();
            }
        }
    }

    /// <summary>
    /// Creates the batched request that merges the items into one aliased operation.
    /// </summary>
    private GraphQLHttpRequest CreateAliasBatchHttpRequest(
        OperationPlanContext context,
        ReadOnlySpan<SourceSchemaClientRequest> requests,
        AliasBatchItem[] items,
        int itemCount,
        List<Utf8VariableDefinitionNode> sharedVariables)
    {
        ref readonly var request = ref requests[0];
        var operation = context.OperationPlan.Operation;
        var body = new AliasBatchedRequestBody(
            items,
            itemCount,
            sharedVariables,
            operation.Name,
            operation.ShortHash,
            ComputeCompositionHash(requests, items, itemCount),
            _onError);

        var httpRequest = new GraphQLHttpRequest(body, _configuration.BaseAddress)
        {
            AcceptHeaderValue = _configuration.BatchingAcceptHeaderValue,
            OperationKind = GetOperationKindHint(request.OperationType),
            HasDeadline = context.HasExecutionDeadline
        };

        SourceSchemaCallbackHelper.ConfigureCallbacks(httpRequest, context, request.Node, _configuration);
        return httpRequest;
    }

    /// <summary>
    /// Computes the hash of the batch composition from the hash of the operation each item is
    /// built from, in item order.
    /// </summary>
    private static ulong ComputeCompositionHash(
        ReadOnlySpan<SourceSchemaClientRequest> requests,
        AliasBatchItem[] items,
        int itemCount)
    {
        var byteCount = itemCount * sizeof(ulong);
        byte[]? rented = null;
        var buffer = byteCount <= 2048
            ? stackalloc byte[byteCount]
            : rented = ArrayPool<byte>.Shared.Rent(byteCount);

        try
        {
            for (var i = 0; i < itemCount; i++)
            {
                var hash = requests[items[i].RequestIndex].OperationSourceText.Hash.Xxx;
                BinaryPrimitives.WriteUInt64LittleEndian(buffer[(i * sizeof(ulong))..], hash);
            }

            return XxHash64.HashToUInt64(buffer[..byteCount]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    /// <summary>
    /// Splits the batched response into one result per item and writes them to
    /// <paramref name="results"/>. A response that carries an outcome for the whole request is
    /// handed to every item, and a response that does not follow the batched shape fails the request.
    /// </summary>
    private static void SplitResponse(
        SourceResultDocument document,
        ReadOnlySpan<AliasBatchItem> items,
        SourceSchemaResult[] results,
        string schemaName)
    {
        switch (AliasBatchResponseSplitter.TrySplit(document, items, results))
        {
            case AliasBatchSplitStatus.Split:
                return;

            case AliasBatchSplitStatus.Broadcast:
                var documentOwner = new SourceResultDocumentOwner(document, items.Length);

                // The outcome applies to the whole request, so every item reports the errors of
                // the response.
                document.Root.TryGetProperty(ErrorsProperty, out var errorsElement);
                var errors = SourceSchemaErrors.From(errorsElement);

                for (var i = 0; i < items.Length; i++)
                {
                    var variables = items[i].Variables;
                    results[i] = new SourceSchemaResult(
                        variables.Path,
                        document,
                        documentOwner: documentOwner,
                        errors: errors,
                        additionalPaths: variables.AdditionalPaths);
                }

                return;

            default:
                throw ThrowHelper.InvalidAliasBatchResponse(schemaName);
        }
    }

    /// <summary>
    /// Flattens the batchable requests into one item per variable set and collects the variable
    /// definitions that the items share.
    /// </summary>
    private static int CollectItems(
        ReadOnlySpan<SourceSchemaClientRequest> requests,
        AliasBatchItem[] items,
        List<Utf8VariableDefinitionNode> sharedVariables)
    {
        var itemCount = 0;

        for (var requestIndex = 0; requestIndex < requests.Length; requestIndex++)
        {
            ref readonly var request = ref requests[requestIndex];

            if (!CanAliasBatch(request, out var operation, out var rootField))
            {
                continue;
            }

            if (!request.ForwardedVariables.IsEmpty)
            {
                CollectSharedVariables(request.ForwardedVariables, operation, sharedVariables);
            }

            var lookupTypeName = request.LookupTypeName!;

            if (request.Variables.IsDefaultOrEmpty)
            {
                items[itemCount++] = new AliasBatchItem(
                    requestIndex, rootField, lookupTypeName, VariableValues.Empty);
                continue;
            }

            foreach (var variables in request.Variables)
            {
                items[itemCount++] = new AliasBatchItem(
                    requestIndex, rootField, lookupTypeName, variables);
            }
        }

        if (sharedVariables.Count > 0)
        {
            DemoteConflictingVariables(requests, sharedVariables);
        }

        return itemCount;
    }

    /// <summary>
    /// Collects the variable definitions of an operation that the request forwards from the client
    /// request, because those carry one value for the whole request and are therefore declared
    /// once for the batched operation. Every other variable of that request carries a value per
    /// item.
    /// </summary>
    private static void CollectSharedVariables(
        ImmutableArray<string> forwardedVariables,
        Utf8OperationDefinitionNode operation,
        List<Utf8VariableDefinitionNode> sharedVariables)
    {
        foreach (var definition in operation.GetVariableDefinitions())
        {
            var name = definition.Utf8Name;

            if (ContainsName(sharedVariables, name) || !ContainsName(forwardedVariables, name))
            {
                continue;
            }

            sharedVariables.Add(definition);
        }
    }

    /// <summary>
    /// Drops every shared variable whose name another request of the batch declares without
    /// forwarding it. The batched operation declares each name once, so a name that one request
    /// carries a value per item for is prefixed for every item of the batch, including the items
    /// of the requests that forward it.
    /// </summary>
    private static void DemoteConflictingVariables(
        ReadOnlySpan<SourceSchemaClientRequest> requests,
        List<Utf8VariableDefinitionNode> sharedVariables)
    {
        foreach (ref readonly var request in requests)
        {
            if (!CanAliasBatch(request, out var operation, out _))
            {
                continue;
            }

            foreach (var definition in operation.GetVariableDefinitions())
            {
                var name = definition.Utf8Name;

                if (!ContainsName(request.ForwardedVariables, name))
                {
                    RemoveName(sharedVariables, name);
                }
            }
        }
    }

    private static bool ContainsName(
        List<Utf8VariableDefinitionNode> sharedVariables,
        ReadOnlySpan<byte> name)
    {
        for (var i = 0; i < sharedVariables.Count; i++)
        {
            if (sharedVariables[i].Utf8Name.SequenceEqual(name))
            {
                return true;
            }
        }

        return false;
    }

    private static void RemoveName(
        List<Utf8VariableDefinitionNode> sharedVariables,
        ReadOnlySpan<byte> name)
    {
        for (var i = 0; i < sharedVariables.Count; i++)
        {
            if (sharedVariables[i].Utf8Name.SequenceEqual(name))
            {
                sharedVariables.RemoveAt(i);
                return;
            }
        }
    }

    private static bool ContainsName(ImmutableArray<string> names, ReadOnlySpan<byte> utf8Name)
    {
        for (var i = 0; i < names.Length; i++)
        {
            if (EqualsName(names[i], utf8Name))
            {
                return true;
            }
        }

        return false;
    }

    // A GraphQL name holds ASCII characters only, so each character is one UTF-8 byte.
    private static bool EqualsName(string name, ReadOnlySpan<byte> utf8Name)
    {
        if (name.Length != utf8Name.Length)
        {
            return false;
        }

        for (var i = 0; i < name.Length; i++)
        {
            if (name[i] != utf8Name[i])
            {
                return false;
            }
        }

        return true;
    }

    private static int CountVariableSets(ImmutableArray<SourceSchemaClientRequest> requests)
    {
        var count = 0;

        foreach (var request in requests)
        {
            count += Math.Max(1, request.Variables.Length);
        }

        return count;
    }

    /// <summary>
    /// Determines whether a request can join a batched request and returns the parts of its
    /// operation the batch is built from. File uploads keep their multipart encoding, mutations
    /// and subscriptions are never batched, and an operation that the plan did not resolve a
    /// lookup type for or that does not read its data from a single unaliased root selection
    /// cannot be rewritten into an aliased copy of itself.
    /// </summary>
    /// <param name="request">The request to inspect.</param>
    /// <param name="operation">The single operation of the request's document.</param>
    /// <param name="rootField">The single root selection of <paramref name="operation"/>.</param>
    private static bool CanAliasBatch(
        in SourceSchemaClientRequest request,
        out Utf8OperationDefinitionNode operation,
        out Utf8FieldNode rootField)
    {
        if (request is
            {
                LookupTypeName: not null,
                OperationType: OperationType.Query,
                RequiresFileUpload: false
            })
        {
            return TryGetSingleRootField(
                request.OperationDocument,
                out operation,
                out rootField);
        }

        operation = default;
        rootField = default;
        return false;
    }

    /// <summary>
    /// Returns the document's single operation together with its single unaliased root field.
    /// </summary>
    private static bool TryGetSingleRootField(
        Utf8OperationDocument document,
        out Utf8OperationDefinitionNode operation,
        out Utf8FieldNode rootField)
    {
        operation = default;
        rootField = default;

        var operations = document.GetOperations().GetEnumerator();

        if (!operations.MoveNext())
        {
            return false;
        }

        operation = operations.Current;

        if (operations.MoveNext())
        {
            return false;
        }

        var selections = operation.SelectionSet.GetSelections().GetEnumerator();

        if (!selections.MoveNext()
            || !selections.Current.TryGetField(out rootField)
            || rootField.HasAlias)
        {
            return false;
        }

        return !selections.MoveNext();
    }
}
