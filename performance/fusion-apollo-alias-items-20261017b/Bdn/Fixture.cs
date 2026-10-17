using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using ApolloErrorVariablesMicro;
using BenchmarkDotNet.Attributes;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace ApolloAliasBdn;

[MemoryDiagnoser]
public class AliasBenchmarks
{
    private const string Lookup = "query($__fusion_1_id: ID!){fieldById(id: $__fusion_1_id){field}}";
    private ServiceProvider _services = null!;
    private IRequestExecutor _executor = null!;
    private OperationPlan _plan = null!;
    private HttpSourceSchemaClient _client = null!;
    private ObjectPool<PooledRequestContext> _requestContexts = null!;
    private OperationPlanContextPool _contexts = null!;
    private IOperationRequest _operation = null!;
    private readonly Handler _handler = new();
    private readonly ChunkedArrayWriter _variables = new();
    private readonly Utf8OperationDocument _document = Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(Lookup));
    private ImmutableArray<SourceSchemaClientRequest>[] _batches = null!;
    private SourceSchemaClientRequest _single;
    private long _completed;

    [Params("ordinary", "alias2", "alias-sequence10")]
    public string Shape { get; set; } = "ordinary";

    internal long CompletedOperations => _completed;

    [GlobalSetup]
    public Task SetupAsync() => InitializeAsync(warm: true);

    public async Task InitializeAsync(bool warm)
    {
        var collection = new ServiceCollection();
        collection.AddHttpClient();
        _services = collection.AddGraphQLGateway()
            .AddInMemoryConfiguration(SchemaFixture.ComposeApolloSchema())
            .Services.BuildServiceProvider();
        _executor = await _services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
        var schema = (FusionSchemaDefinition)_executor.Schema;
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var rewritten = new DocumentRewriter(schema).RewriteDocument(
            Utf8GraphQLParser.Parse("{ products { id } }"), operationName: null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().Single();
        _plan = new OperationPlanner(schema, new OperationCompiler(schema, pool))
            .CreatePlan("alias-benchmark", "alias-benchmark", "alias-benchmark", operation);
        _operation = OperationRequestBuilder.New().SetDocument("{ products { id } }").Build();
        _requestContexts = schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
        _contexts = schema.Services.GetRequiredService<OperationPlanContextPool>();
        _client = new HttpSourceSchemaClient(
            new DefaultGraphQLHttpClient(new HttpClient(_handler), disposeInnerClient: true),
            new HttpSourceSchemaClientConfiguration("a", new Uri("http://localhost/graphql"),
                capabilities: SourceSchemaClientCapabilities.AliasBatching));
        _single = CreateRequest(1);
        var counts = Shape switch
        {
            "ordinary" => new[] { 1 },
            "alias2" => new[] { 2 },
            "alias-sequence10" => new[] { 2, 3, 2, 3 },
            _ => throw new InvalidOperationException("Unexpected shape.")
        };
        _batches = counts.Select(count => Enumerable.Range(0, count)
            .Select(_ => CreateRequest(1)).ToImmutableArray()).ToArray();
        _handler.Capture = true;
        await ExecuteOneAsync();
        var captured = _handler.Bodies.ToArray();
        for (var batch = 0; batch < captured.Length; batch++)
        {
            using var json = JsonDocument.Parse(captured[batch]);
            var variables = json.RootElement.GetProperty("variables");
            if (variables.EnumerateObject().Count() != counts[batch])
            {
                throw new InvalidOperationException("Unexpected serialized variable count.");
            }
            for (var item = 0; item < counts[batch]; item++)
            {
                var name = Shape == "ordinary" ? "__fusion_1_id" : $"_{item}___fusion_1_id";
                if (variables.GetProperty(name).GetString() != "1")
                {
                    throw new InvalidOperationException("Unexpected serialized variable value.");
                }
            }
        }
        if (captured.Length != counts.Length)
        {
            throw new InvalidOperationException("Unexpected HTTP dispatch count.");
        }
        Console.WriteLine(JsonSerializer.Serialize(new { Shape, requests = captured, expectedResults = counts.Sum() }));
        _handler.Capture = false;
        _handler.Bodies.Clear();
        if (warm)
        {
            var start = Stopwatch.GetTimestamp();
            do
            {
                await ExecuteOneAsync();
            }
            while (Stopwatch.GetElapsedTime(start).TotalSeconds < 3);
        }
    }

    private SourceSchemaClientRequest CreateRequest(int count)
    {
        var values = ImmutableArray.CreateBuilder<VariableValues>(count);
        for (var i = 0; i < count; i++)
        {
            var start = _variables.Position;
            var bytes = "{\"__fusion_1_id\":\"1\"}"u8;
            bytes.CopyTo(_variables.GetSpan(bytes.Length));
            _variables.Advance(bytes.Length);
            values.Add(new VariableValues(CompactPath.Root,
                JsonSegment.Create(_variables, start, _variables.Position - start)));
        }
        var source = Encoding.UTF8.GetBytes(Lookup);
        return new SourceSchemaClientRequest
        {
            Node = _plan.RootNodes[0], SchemaName = "a", OperationType = OperationType.Query,
            OperationSourceText = new OperationSourceText("lookup", OperationType.Query, source,
                OperationSourceTextHash.Compute(source)),
            Variables = values.MoveToImmutable(), OperationDocument = _document, LookupTypeName = "Foo"
        };
    }

    [Benchmark(OperationsPerInvoke = 1000)]
    public async Task ExecuteAsync()
    {
        var before = _completed;
        for (var i = 0; i < 1000; i++)
        {
            await ExecuteOneAsync();
        }
        if (_completed - before != 1000)
        {
            throw new InvalidOperationException("Incomplete benchmark invocation.");
        }
    }

    public async ValueTask ExecuteOneAsync()
    {
        using var arena = new MemoryArena();
        using var cancellation = new CancellationTokenSource();
        var request = _requestContexts.Get();
        var context = _contexts.Rent();
        request.Initialize(_executor.Schema, _executor.Version, _operation, 0, _services, CancellationToken.None);
        try
        {
            context.Initialize(request, VariableValueCollection.Empty, _plan, cancellation, arena);
            var count = 0;
            if (Shape == "ordinary")
            {
                _handler.SetResponse(0);
                await foreach (var result in _client.ExecuteAsync(context, _single, CancellationToken.None))
                {
                    using (result)
                    {
                        if (result.Data.GetProperty("fieldById").GetProperty("field").GetString() != "A")
                        {
                            throw new InvalidOperationException("Invalid native result.");
                        }
                        count++;
                    }
                }
            }
            else
            {
                foreach (var batch in _batches)
                {
                    _handler.SetResponse(batch.Length);
                    var index = 0;
                    await foreach (var result in _client.ExecuteBatchAsync(context, batch, CancellationToken.None))
                    {
                        using (result.Result)
                        {
                            if (result.RequestIndex != index++
                                || result.Result.LookupData!.Value.GetProperty("field").GetString() != "A")
                            {
                                throw new InvalidOperationException("Invalid alias result.");
                            }
                            count++;
                        }
                    }
                }
            }
            if (count != (Shape == "alias-sequence10" ? 10 : Shape == "alias2" ? 2 : 1))
            {
                throw new InvalidOperationException("Incomplete result enumeration.");
            }
            _completed++;
        }
        finally
        {
            arena.Seal();
            await context.DisposeAsync();
            _requestContexts.Return(request);
        }
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }
        _document.Dispose();
        _variables.Dispose();
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }
    }

    private sealed class Handler : HttpMessageHandler
    {
        private readonly byte[][] _responses = Enumerable.Range(0, 4).Select(count =>
            Encoding.UTF8.GetBytes(count == 0
                ? "{\"data\":{\"fieldById\":{\"field\":\"A\"}}}"
                : "{\"data\":{" + string.Join(",", Enumerable.Range(0, count)
                    .Select(i => $"\"_{i}_fieldById\":{{\"field\":\"A\"}}")) + "}}"))
            .ToArray();
        private byte[] _response = null!;
        public bool Capture { get; set; }
        public List<string> Bodies { get; } = [];
        public void SetResponse(int count) => _response = _responses[count];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Capture)
            {
                Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            }
            else
            {
                await request.Content!.CopyToAsync(Stream.Null, cancellationToken);
            }
            var content = new ByteArrayContent(_response);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }
    }
}
