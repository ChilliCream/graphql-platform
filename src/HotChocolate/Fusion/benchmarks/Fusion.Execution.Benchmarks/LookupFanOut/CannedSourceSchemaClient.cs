using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Benchmarks.LookupFanOut;

internal sealed class CannedSourceSchemaClientConfiguration(string name) : ISourceSchemaClientConfiguration
{
    public string Name { get; } = name;

    public SupportedOperationType SupportedOperations => SupportedOperationType.All;
}

internal sealed class CannedSourceSchemaClientFactory
    : SourceSchemaClientFactory<CannedSourceSchemaClientConfiguration>
{
    private readonly CannedResponses _responses = new();

    protected override ISourceSchemaClient CreateClient(
        FusionSchemaDefinition schema,
        CannedSourceSchemaClientConfiguration configuration)
        => new CannedSourceSchemaClient(_responses);
}

/// <summary>
/// Caches the response bytes per operation and variable set, so a benchmark invocation only pays
/// for a hash lookup and the parse of the response into the request arena.
/// </summary>
internal sealed class CannedResponses
{
    private readonly ConcurrentDictionary<(ulong Operation, ulong Variables), byte[]> _responses = new();
    private readonly ConcurrentDictionary<ulong, DocumentNode> _documents = new();

    public byte[] Get(SourceSchemaClientRequest request, VariableValues? variables)
    {
        var operationHash = request.OperationSourceText.Hash.Xxx;
        var hasher = new XxHash64();

        if (variables is { } v && !v.Values.IsEmpty)
        {
            foreach (var segment in v.Values.AsSequence())
            {
                hasher.Append(segment.Span);
            }
        }

        var key = (operationHash, hasher.GetCurrentHashAsUInt64());

        if (_responses.TryGetValue(key, out var response))
        {
            return response;
        }

        var variableBytes = variables is { } w && !w.Values.IsEmpty
            ? w.Values.AsSequence().ToArray()
            : [];
        var document = _documents.GetOrAdd(
            operationHash,
            _ => Utf8GraphQLParser.Parse(request.OperationSourceText.Value.Span));

        using var variableDocument = JsonDocument.Parse(variableBytes.Length == 0 ? "{}"u8.ToArray() : variableBytes);
        response = BenchmarkSubgraphData.CreateResponse(document, variableDocument.RootElement.Clone());
        _responses.TryAdd(key, response);
        return response;
    }
}

/// <summary>
/// A source schema client that answers from <see cref="CannedResponses"/> and hands results to the
/// gateway the same way the in-memory connector does: one parsed document per variable set.
/// </summary>
internal sealed class CannedSourceSchemaClient(CannedResponses responses) : ISourceSchemaClient
{
    public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.Default;

    public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        foreach (var result in CreateResults(context, request))
        {
            yield return result;
        }
    }

    public async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        for (var i = 0; i < requests.Length; i++)
        {
            foreach (var result in CreateResults(context, requests[i]))
            {
                yield return new SourceSchemaBatchResult(i, result);
            }
        }
    }

    public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private List<SourceSchemaResult> CreateResults(
        OperationPlanContext context,
        SourceSchemaClientRequest request)
    {
        var results = new List<SourceSchemaResult>(Math.Max(1, request.Variables.Length));

        if (request.Variables.Length == 0)
        {
            results.Add(new SourceSchemaResult(CompactPath.Root, Parse(context, responses.Get(request, null))));
            return results;
        }

        foreach (var variables in request.Variables)
        {
            var document = Parse(context, responses.Get(request, variables));

            results.Add(
                variables.AdditionalPaths.IsDefaultOrEmpty
                    ? new SourceSchemaResult(variables.Path, document)
                    : new SourceSchemaResult(variables.Path, document, additionalPaths: variables.AdditionalPaths));
        }

        return results;
    }

    private static SourceResultDocument Parse(OperationPlanContext context, byte[] response)
        => SourceResultDocument.Parse(context.Memory, response, response.Length);
}
