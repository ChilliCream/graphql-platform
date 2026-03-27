using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Transport.Formatters;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// An <see cref="ISourceSchemaClient"/> implementation that executes GraphQL operations
/// directly in-process via <see cref="RequestExecutorProxy"/>, bypassing any network transport.
/// </summary>
public sealed class InMemorySourceSchemaClient : ISourceSchemaClient
{
    private static readonly Uri s_uri = new("inmemory://localhost");

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
        => default;

    /// <inheritdoc />
    public async ValueTask<SourceSchemaClientResponse> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (request.Variables.Length > 1)
        {
            throw new InvalidOperationException(
                "The in-memory source schema client does not support variable batching.");
        }

        var operationRequest = BuildOperationRequest(request);

        try
        {
            var result = await _executor
                .ExecuteAsync(operationRequest, cancellationToken)
                .ConfigureAwait(false);

            return new Response(result, request, _formatter);
        }
        catch
        {
            operationRequest.Dispose();
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

        for (var i = 0; i < requests.Length; i++)
        {
            var response = await ExecuteAsync(context, requests[i], cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await foreach (var sourceResult in response
                    .ReadAsResultStreamAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    yield return new BatchStreamResult(i, sourceResult);
                }
            }
            finally
            {
                response.Dispose();
            }
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

    private static OperationRequest BuildOperationRequest(SourceSchemaClientRequest request)
    {
        JsonDocument? variables = null;

        if (request.Variables.Length == 1 && !request.Variables[0].IsEmpty)
        {
            var sequence = request.Variables[0].Values.AsSequence();
            variables = JsonDocument.Parse(sequence);
        }

        return OperationRequest.FromSourceText(
            request.OperationSourceText,
            variableValues: variables);
    }

    private static SourceResultDocument SerializeToDocument(
        OperationResult operationResult,
        JsonResultFormatter formatter)
    {
        var writer = new ChunkedArrayWriter(JsonMemoryKind.Json);

        try
        {
            formatter.Format(operationResult, writer);
            var (chunks, usedChunks, lastLength) = writer.DrainChunks();
            return SourceResultDocument.Parse(chunks, lastLength, usedChunks, pooledMemory: true);
        }
        catch
        {
            writer.Dispose();
            throw;
        }
    }

    private sealed class Response : SourceSchemaClientResponse
    {
        private readonly IExecutionResult _result;
        private readonly SourceSchemaClientRequest _request;
        private readonly JsonResultFormatter _formatter;

        public Response(
            IExecutionResult result,
            SourceSchemaClientRequest request,
            JsonResultFormatter formatter)
        {
            _result = result;
            _request = request;
            _formatter = formatter;
        }

        public override Uri Uri => s_uri;

        public override string ContentType => "application/json";

        public override bool IsSuccessful => true;

        public override async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            switch (_result)
            {
                case OperationResult operationResult:
                {
                    var path = _request.Variables.Length == 1
                        ? _request.Variables[0].Path
                        : CompactPath.Root;

                    var additionalPaths = _request.Variables.Length == 1
                        ? _request.Variables[0].AdditionalPaths
                        : default;

                    var document = SerializeToDocument(operationResult, _formatter);

                    yield return additionalPaths.IsDefaultOrEmpty
                        ? new SourceSchemaResult(path, document)
                        : new SourceSchemaResult(path, document, additionalPaths: additionalPaths);
                    break;
                }

                case IResponseStream responseStream:
                {
                    await foreach (var operationResult in responseStream
                        .ReadResultsAsync()
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        var document = SerializeToDocument(operationResult, _formatter);
                        yield return new SourceSchemaResult(CompactPath.Root, document);
                    }
                    break;
                }

                default:
                    throw new InvalidOperationException(
                        $"Unexpected execution result type: {_result.GetType().Name}.");
            }
        }

        public override void Dispose()
        {
            _result.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
