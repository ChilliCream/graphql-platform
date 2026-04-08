using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Text.Json;
using HotChocolate.Transport.Formatters;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// An <see cref="ISourceSchemaClient"/> implementation that executes GraphQL operations
/// directly in-process via <see cref="RequestExecutorProxy"/>, bypassing any network transport.
/// </summary>
public sealed class InMemorySourceSchemaClient : ISourceSchemaClient
{
    private static readonly Uri s_uri = new("inmemory://localhost");
    private static readonly byte[] s_fileMarkerPrefix = "$.file("u8.ToArray();

    private static readonly JsonWriterOptions s_jsonWriterOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly RequestExecutorProxy _executor;
    private readonly JsonResultFormatter _formatter;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemorySourceSchemaClient"/>.
    /// </summary>
    /// <param name="executor">
    /// The request executor proxy for the source schema.
    /// </param>
    /// <param name="formatter">
    /// The JSON result formatter used to serialize execution results.
    /// </param>
    public InMemorySourceSchemaClient(
        RequestExecutorProxy executor,
        JsonResultFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(formatter);

        _executor = executor;
        _formatter = formatter;
    }

    /// <inheritdoc />
    public SourceSchemaClientCapabilities Capabilities
        => SourceSchemaClientCapabilities.VariableBatching
            | SourceSchemaClientCapabilities.RequestBatching;

    /// <inheritdoc />
    public async ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ObjectDisposedException.ThrowIf(_disposed, this);

        ChunkedArrayWriter? buffer = null;
        var operationRequest = BuildOperationRequest(context, request, ref buffer);

        try
        {
            var result = await _executor
                .ExecuteAsync(operationRequest, cancellationToken)
                .ConfigureAwait(false);

            return new Response(result, request, _formatter, buffer);
        }
        catch
        {
            operationRequest.Dispose();
            buffer?.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<BatchStreamResult> ExecuteBatchStreamAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Build one IOperationRequest per source request.
        var operationRequests = new IOperationRequest[requests.Length];
        ChunkedArrayWriter? buffer = null;

        try
        {
            for (var i = 0; i < requests.Length; i++)
            {
                operationRequests[i] = BuildOperationRequest(context, requests[i], ref buffer);
            }
        }
        catch
        {
            for (var i = 0; i < operationRequests.Length; i++)
            {
                operationRequests[i]?.Dispose();
            }

            buffer?.Dispose();
            throw;
        }

        var batch = new OperationRequestBatch(operationRequests);
        IResponseStream responseStream;

        try
        {
            responseStream = await _executor
                .ExecuteBatchAsync(batch, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            buffer?.Dispose();
            throw;
        }

        try
        {
            await foreach (var operationResult in responseStream
                .ReadResultsAsync()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                var requestIndex = ResolveRequestIndex(requests, operationResult);

                if (requestIndex == -1)
                {
                    // No request index — fan out to all requests.
                    var document = SerializeToDocument(operationResult, _formatter);

                    for (var i = 0; i < requests.Length; i++)
                    {
                        if (TryGetResultPath(requests[i], variableIndex: 0, out var p, out var ap))
                        {
                            var ssr = ap.IsDefaultOrEmpty
                                ? new SourceSchemaResult(p, document)
                                : new SourceSchemaResult(p, document, additionalPaths: ap);

                            yield return new BatchStreamResult(i, ssr);
                        }
                    }

                    continue;
                }

                var request = requests[requestIndex];
                var variableIndex = ResolveVariableIndex(request, operationResult);

                if (variableIndex == -1)
                {
                    // No variable index — fan out to all variable sets of this request.
                    var document = SerializeToDocument(operationResult, _formatter);

                    for (var vi = 0; vi < request.Variables.Length; vi++)
                    {
                        if (TryGetResultPath(request, vi, out var vp, out var vap))
                        {
                            var vssr = vap.IsDefaultOrEmpty
                                ? new SourceSchemaResult(vp, document)
                                : new SourceSchemaResult(vp, document, additionalPaths: vap);

                            yield return new BatchStreamResult(requestIndex, vssr);
                        }
                    }

                    continue;
                }

                if (!TryGetResultPath(request, variableIndex, out var path, out var additionalPaths))
                {
                    throw new InvalidOperationException(
                        $"Invalid variable index {variableIndex} for request {requestIndex}.");
                }

                var resultDocument = SerializeToDocument(operationResult, _formatter);

                var sourceSchemaResult = additionalPaths.IsDefaultOrEmpty
                    ? new SourceSchemaResult(path, resultDocument)
                    : new SourceSchemaResult(path, resultDocument, additionalPaths: additionalPaths);

                yield return new BatchStreamResult(requestIndex, sourceSchemaResult);
            }
        }
        finally
        {
            await responseStream.DisposeAsync().ConfigureAwait(false);
            buffer?.Dispose();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _executor.Dispose();
            _disposed = true;
        }

        return ValueTask.CompletedTask;
    }

    private static IOperationRequest BuildOperationRequest(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        ref ChunkedArrayWriter? buffer)
    {
        IFeatureCollection? features = null;

        if (request.RequiresFileUpload)
        {
            features = new FeatureCollection();
            features.Set(context.RequestContext.Features.GetRequired<IFileLookup>());
        }

        if (request.Variables.Length == 0)
        {
            return OperationRequest.FromSourceText(
                request.OperationSourceText,
                features: features);
        }

        if (request.Variables.Length == 1)
        {
            var sequence = request.Variables[0].Values.AsSequence();

            if (!request.RequiresFileUpload)
            {
                return OperationRequest.FromSourceText(
                    request.OperationSourceText,
                    variableValues: JsonDocument.Parse(sequence));
            }

            buffer ??= new ChunkedArrayWriter();
            var cleanedJson = StripFileMarkers(buffer, sequence);
            return OperationRequest.FromSourceText(
                request.OperationSourceText,
                variableValues: JsonDocument.Parse(cleanedJson.AsSequence()),
                features: features);
        }

        buffer ??= new ChunkedArrayWriter();
        var writer = new JsonWriter(buffer, s_jsonWriterOptions);

        writer.WriteStartArray();

        for (var i = 0; i < request.Variables.Length; i++)
        {
            request.Variables[i].Values.WriteTo(writer);
        }

        writer.WriteEndArray();

        var variables = JsonSegment.Create(buffer, 0, buffer.Length);

        var variableSequence = request.RequiresFileUpload
            ? StripFileMarkers(buffer, variables.AsSequence()).AsSequence()
            : variables.AsSequence();

        return VariableBatchRequest.FromSourceText(
            request.OperationSourceText,
            variableValues: JsonDocument.Parse(variableSequence),
            features: features);
    }

    /// <summary>
    /// Scans JSON for <c>$.file(key)</c> string markers and replaces them with just <c>key</c>.
    /// </summary>
    private static JsonSegment StripFileMarkers(
        ChunkedArrayWriter buffer,
        ReadOnlySequence<byte> json)
    {
        var reader = new Utf8JsonReader(json, isFinalBlock: true, default);
        var startPosition = buffer.Position;
        var writer = new JsonWriter(buffer, s_jsonWriterOptions);

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    writer.WriteStartObject();
                    break;

                case JsonTokenType.EndObject:
                    writer.WriteEndObject();
                    break;

                case JsonTokenType.StartArray:
                    writer.WriteStartArray();
                    break;

                case JsonTokenType.EndArray:
                    writer.WriteEndArray();
                    break;

                case JsonTokenType.PropertyName:
                    writer.WritePropertyName(reader.ValueSpan);
                    break;

                case JsonTokenType.String:
                    var span = reader.ValueSpan;
                    if (span.Length > s_fileMarkerPrefix.Length + 1
                        && span.StartsWith(s_fileMarkerPrefix)
                        && span[^1] == (byte)')')
                    {
                        // $.file(key) → key
                        span = span.Slice(s_fileMarkerPrefix.Length, span.Length - s_fileMarkerPrefix.Length - 1);
                        writer.WriteStringValue(span, skipEscaping: true);
                    }
                    else
                    {
                        writer.WriteStringValue(span, skipEscaping: true);
                    }
                    break;

                case JsonTokenType.Number:
                    writer.WriteNumberValue(reader.ValueSpan);
                    break;

                case JsonTokenType.True:
                    writer.WriteBooleanValue(true);
                    break;

                case JsonTokenType.False:
                    writer.WriteBooleanValue(false);
                    break;

                case JsonTokenType.Null:
                    writer.WriteNullValue();
                    break;
            }
        }

        return JsonSegment.Create(buffer, startPosition, buffer.Position - startPosition);
    }

    private static int ResolveRequestIndex(
        ImmutableArray<SourceSchemaClientRequest> requests,
        OperationResult result)
        => requests.Length == 1 ? 0 : result.RequestIndex ?? -1;

    private static int ResolveVariableIndex(
        SourceSchemaClientRequest request,
        OperationResult result)
        => request.Variables.Length <= 1 ? 0 : result.VariableIndex ?? -1;

    private static bool TryGetResultPath(
        SourceSchemaClientRequest request,
        int variableIndex,
        out CompactPath path,
        out CompactPathSegment additionalPaths)
    {
        if (request.Variables.Length == 0)
        {
            path = CompactPath.Root;
            additionalPaths = default;
            return true;
        }

        if ((uint)variableIndex >= (uint)request.Variables.Length)
        {
            path = CompactPath.Root;
            additionalPaths = default;
            return false;
        }

        var variable = request.Variables[variableIndex];
        path = variable.Path;
        additionalPaths = variable.AdditionalPaths;
        return true;
    }

    private static SourceResultDocument SerializeToDocument(
        OperationResult operationResult,
        JsonResultFormatter formatter)
    {
        using var writer = new ChunkedArrayWriter(JsonMemoryKind.Json);
        formatter.Format(operationResult, writer);
        var (chunks, usedChunks, lastLength) = writer.DrainChunks();
        return SourceResultDocument.Parse(chunks, lastLength, usedChunks, pooledMemory: true);
    }

    private sealed class Response : SourceSchemaClientResponse
    {
        private readonly IExecutionResult _result;
        private readonly SourceSchemaClientRequest _request;
        private readonly JsonResultFormatter _formatter;
        private readonly ChunkedArrayWriter? _buffer;

        public Response(
            IExecutionResult result,
            SourceSchemaClientRequest request,
            JsonResultFormatter formatter,
            ChunkedArrayWriter? buffer)
        {
            _result = result;
            _request = request;
            _formatter = formatter;
            _buffer = buffer;
        }

        public override Uri Uri => s_uri;

        public override string ContentType => "application/json";

        public override bool IsSuccessful => true;

        public override async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var variables = _request.Variables;

            if (_request.OperationType == OperationType.Subscription)
            {
                if (_result is OperationResult errorResult)
                {
                    var document = SerializeToDocument(errorResult, _formatter);
                    yield return new SourceSchemaResult(CompactPath.Root, document);
                }
                else
                {
                    var stream = _result.ExpectResponseStream();

                    await foreach (var operationResult in stream
                        .ReadResultsAsync()
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        var document = SerializeToDocument(operationResult, _formatter);
                        yield return new SourceSchemaResult(CompactPath.Root, document);
                    }
                }

                yield break;
            }

            switch (variables.Length)
            {
                case 0:
                {
                    var document = SerializeToDocument(_result.ExpectOperationResult(), _formatter);
                    yield return new SourceSchemaResult(CompactPath.Root, document);
                    break;
                }

                case 1:
                {
                    var document = SerializeToDocument(_result.ExpectOperationResult(), _formatter);
                    var variable = variables[0];

                    yield return variable.AdditionalPaths.IsDefaultOrEmpty
                        ? new SourceSchemaResult(variable.Path, document)
                        : new SourceSchemaResult(variable.Path, document, additionalPaths: variable.AdditionalPaths);
                    break;
                }

                default:
                {
                    if (_result is OperationResult singleResult)
                    {
                        // Single result for all variable sets (e.g. validation error).
                        var document = SerializeToDocument(singleResult, _formatter);
                        var errorResult = new SourceSchemaResult(variables[0].Path, document);

                        for (var i = 0; i < variables.Length; i++)
                        {
                            var variable = variables[i];
                            yield return errorResult.WithPath(variable.Path, variable.AdditionalPaths);
                        }
                    }
                    else
                    {
                        // Variable batching — one result per variable set.
                        var resultBatch = (OperationResultBatch)_result;

                        for (var i = 0; i < resultBatch.Results.Count; i++)
                        {
                            if (resultBatch.Results[i] is not OperationResult operationResult)
                            {
                                continue;
                            }

                            if (operationResult.VariableIndex is not { } index
                                || (uint)index >= (uint)variables.Length)
                            {
                                throw new InvalidOperationException(
                                    "The operation result is missing a valid variable index.");
                            }

                            var variable = variables[index];
                            var document = SerializeToDocument(operationResult, _formatter);

                            yield return variable.AdditionalPaths.IsDefaultOrEmpty
                                ? new SourceSchemaResult(variable.Path, document)
                                : new SourceSchemaResult(variable.Path, document, additionalPaths: variable.AdditionalPaths);
                        }
                    }

                    break;
                }
            }
        }

        public override void Dispose()
        {
            _result.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _buffer?.Dispose();
        }
    }
}
