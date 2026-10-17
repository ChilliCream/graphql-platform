using HotChocolate;
using HotChocolate.Buffers;
using HotChocolate.Transport.Formatters;
using HotChocolate.Fusion;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApolloErrorVariablesMicro;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace ApolloExecutorBdn;

internal sealed record Case(string Name, int Count, int Unique, bool Batch, string Response, bool Ordinary = false);

internal sealed class FixtureClient : ISourceSchemaClient
{
    private readonly Case _shape;
    private readonly byte[] _root;
    private readonly byte[] _entities;
    private readonly byte[] _categories;
    public FixtureClient(Case shape)
    {
        _shape = shape;
        var data = new Dictionary<string, object>
        {
            ["products"] = Enumerable.Range(0, shape.Count).Select(i => new
            {
                __typename = "Product", id = (i % shape.Unique + 1).ToString()
            }).ToArray()
        };
        if (shape.Batch)
        {
            data["categories"] = Enumerable.Range(0, shape.Count).Select(i => new
            {
                __typename = "Category", id = (i % shape.Unique + 1).ToString()
            }).ToArray();
        }
        _root = JsonSerializer.SerializeToUtf8Bytes(new { data });
        _entities = JsonSerializer.SerializeToUtf8Bytes(new
        {
            data = new
            {
                _entities = Enumerable.Range(1, shape.Unique).Select(i => new
                {
                    name = "Name" + i
                }).ToArray()
            }
        });
        _categories = JsonSerializer.SerializeToUtf8Bytes(new
        {
            data = new
            {
                _entities = Enumerable.Range(1, shape.Unique).Select(i => new
                {
                    title = "Name" + i
                }).ToArray()
            }
        });
    }
    public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;
    public int OwnerThread { get; set; }
    public bool Capturing { get; set; } = true;
    public List<string> CapturedVariables { get; } = [];
    public bool OtherThreadObserved { get; private set; }
    public long SingleCalls { get; private set; }
    public long BatchCalls { get; private set; }
    public long BatchRequests { get; private set; }

    public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
        OperationPlanContext context,
        SourceSchemaClientRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        OtherThreadObserved |= OwnerThread != 0 && Environment.CurrentManagedThreadId != OwnerThread;
        var entity = request.Node is ApolloOperationExecutionNode;
        if (entity)
        {
            SingleCalls++;
            Capture(request);
            if (_shape.Response is "transport")
            {
                throw new InvalidOperationException("Source transport failed.");
            }
        }
        var bytes = entity ? _entities : _root;
        yield return new SourceSchemaResult(
            CompactPath.Root, SourceResultDocument.Parse(context.Memory, bytes, bytes.Length));
        await Task.CompletedTask;
    }

    public async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
        OperationPlanContext context,
        ImmutableArray<SourceSchemaClientRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        OtherThreadObserved |= OwnerThread != 0 && Environment.CurrentManagedThreadId != OwnerThread;
        if (requests[0].Node is not ApolloOperationBatchExecutionNode)
        {
            throw new InvalidOperationException("Expected Apollo batch node.");
        }
        BatchCalls++;
        BatchRequests += requests.Length;
        for (var i = 0; i < requests.Length; i++)
        {
            Capture(requests[i]);
            if (requests[i].LookupTypeName is "Product" && _shape.Response is "fallback")
            {
                context.TrackBatchRequestError(requests[i].Node, i,
                    new InvalidOperationException("Source fallback failed."));
                continue;
            }
            var bytes = requests[i].LookupTypeName is "Category" ? _categories : _entities;
            yield return new SourceSchemaBatchResult(i, new SourceSchemaResult(
                CompactPath.Root, SourceResultDocument.Parse(context.Memory, bytes, bytes.Length)));
        }
        await Task.CompletedTask;
    }

    private void Capture(SourceSchemaClientRequest request)
    {
        if (!Capturing)
        {
            return;
        }
        if (request.Variables.Length != 1)
        {
            throw new InvalidOperationException("Expected one combined variable set.");
        }
        using var values = JsonDocument.Parse(request.Variables[0].Values.AsSequence());
        if (values.RootElement.GetProperty("representations").GetArrayLength() != _shape.Unique)
        {
            throw new InvalidOperationException("Wrong unique representation count.");
        }
        CapturedVariables.Add(values.RootElement.GetRawText());
    }

    public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
        OperationPlanContext context, SourceSchemaClientRequest request, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class ClientFactory(ISourceSchemaClient client) : ISourceSchemaClientFactory
{
    public bool CanHandle(ISourceSchemaClientConfiguration configuration) => configuration is ClientConfiguration;
    public ISourceSchemaClient CreateClient(FusionSchemaDefinition schema, ISourceSchemaClientConfiguration configuration) => client;
}

internal sealed class ClientConfiguration(string name) : ISourceSchemaClientConfiguration
{
    public string Name { get; } = name;
    public SupportedOperationType SupportedOperations => SupportedOperationType.Query;
}

