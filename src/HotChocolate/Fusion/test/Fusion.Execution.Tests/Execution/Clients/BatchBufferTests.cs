using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using CookieCrumble;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using FusionOperationRequest = HotChocolate.Fusion.Transport.OperationRequest;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class BatchBufferTests : FusionTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_Should_PreserveCleanupContext_When_ResultCallbackChangesAmbientState(
        bool callbackThrows)
    {
        // arrange
        await using var fixture = await BatchBufferTestFixture.CreateAsync();
        var events = new List<string>();
        var ambient = new AsyncLocal<string?> { Value = "caller" };
        using var callerActivity = new Activity("caller").Start();
        using var callbackActivity = new Activity("callback");
        var content = new AmbientTrackingContent(() =>
            events.Add($"dispose:{ambient.Value}:{Activity.Current?.OperationName}"));
        using var graphQLClient = new DefaultGraphQLHttpClient(
            new HttpClient(new ResponseHandler(content)), disposeInnerClient: true);
        var failure = new InvalidOperationException("callback failed");
        await using var client = new HttpSourceSchemaClient(graphQLClient,
            new HttpSourceSchemaClientConfiguration("A", new Uri("http://localhost/graphql"),
                onSourceSchemaResult: (_, _, _) =>
                {
                    ambient.Value = "callback";
                    callbackActivity.Start();
                    events.Add($"callback:{ambient.Value}:{Activity.Current?.OperationName}");
                    if (callbackThrows)
                    {
                        throw failure;
                    }
                }));

        // act
        await using (var enumerator = client.ExecuteAsync(
            fixture.CreateContext(), CreateRequest(fixture.RootNode), TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken))
        {
            if (callbackThrows)
            {
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => enumerator.MoveNextAsync().AsTask());
                Assert.Same(failure, exception);
            }
            else
            {
                Assert.True(await enumerator.MoveNextAsync());
                events.Add($"consume:{ambient.Value}:{Activity.Current?.OperationName}");
                enumerator.Current.Dispose();
            }
        }

        // assert
        Assert.Equal(
            callbackThrows
                ? ["callback:callback:callback", "dispose:caller:caller"]
                : ["callback:callback:callback", "consume:caller:caller", "dispose:caller:caller"],
            events);
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(0, false, true)]
    [InlineData(0, true, false)]
    [InlineData(0, true, true)]
    [InlineData(1, false, false)]
    [InlineData(1, false, true)]
    [InlineData(1, true, false)]
    [InlineData(1, true, true)]
    public async Task ExecuteAsync_Should_PreserveResultAndCallbacks_When_SingleResultIsRead(
        int variableSets,
        bool nullData,
        bool callbackEnabled)
    {
        // arrange
        await using var fixture = await BatchBufferTestFixture.CreateAsync();
        var events = new List<string>();
        var content = new TrackingContent(
            nullData
                ? """{"data":null,"errors":[{"message":"failure"}],"extensions":{"code":7}}"""
                : """{"data":{"field":"value"},"errors":[{"message":"failure"}],"extensions":{"code":7}}""",
            events);
        using var graphQLClient = new DefaultGraphQLHttpClient(
            new HttpClient(new ResponseHandler(content)), disposeInnerClient: true);
        await using var client = new HttpSourceSchemaClient(graphQLClient,
            new HttpSourceSchemaClientConfiguration("A", new Uri("http://localhost/graphql"),
                onBeforeSend: (_, _, _) => events.Add("before"),
                onAfterReceive: (_, _, _) => events.Add("after"),
                onSourceSchemaResult: callbackEnabled ? (context, node, result) =>
                {
                    var transport = context.GetTransportDetails(node);
                    events.Add($"result:{transport.Uri}:{transport.ContentType}:{result.HasErrors}");
                } : null));
        var context = fixture.CreateContext();
        var path = new CompactPath([1, 2]);
        var additionalPaths = new CompactPathSegment([new CompactPath([1, 3])], 0, 1);
        var request = CreateRequest(fixture.RootNode) with
        {
            Variables = variableSets == 0
                ? []
                : [new VariableValues(path, JsonSegment.Empty) { AdditionalPaths = additionalPaths }]
        };

        // act
        await foreach (var result in client.ExecuteAsync(
            context, request, TestContext.Current.CancellationToken))
        {
            events.Add($"consume:{result.Data.ValueKind}:{result.Errors!.RootErrors[0].Message}:"
                + result.Extensions.GetProperty("code").GetInt32());
            Assert.Equal(variableSets == 0 ? CompactPath.Root : path, result.Path);
            Assert.Equal(variableSets == 0 ? [] : additionalPaths.AsSpan().ToArray(),
                result.AdditionalPaths.AsSpan().ToArray());
            result.Dispose();
            break;
        }

        // assert
        Assert.Equal(
            callbackEnabled
                ? ["before", "after", "result:http://localhost/graphql:application/json; charset=utf-8:True",
                    $"consume:{(nullData ? "Null" : "Object")}:failure:7", "disposed"]
                : ["before", "after", $"consume:{(nullData ? "Null" : "Object")}:failure:7", "disposed"],
            events);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ExecuteAsync_Should_DisposeResponse_When_ResultCallbackThrows(int variableSets)
    {
        // arrange
        await using var fixture = await BatchBufferTestFixture.CreateAsync();
        var events = new List<string>();
        using var graphQLClient = new DefaultGraphQLHttpClient(
            new HttpClient(new ResponseHandler(new TrackingContent("""{"data":null}""", events))),
            disposeInnerClient: true);
        var failure = new InvalidOperationException("callback failed");
        await using var client = new HttpSourceSchemaClient(graphQLClient,
            new HttpSourceSchemaClientConfiguration("A", new Uri("http://localhost/graphql"),
                onSourceSchemaResult: (_, _, _) => throw failure));
        var request = CreateRequest(fixture.RootNode) with
        {
            Variables = variableSets == 0 ? [] : [VariableValues.Empty]
        };
        await using var enumerator = client.ExecuteAsync(
            fixture.CreateContext(), request, TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await enumerator.MoveNextAsync());

        // assert
        Assert.Same(failure, exception);
        Assert.Equal(["disposed"], events);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExecuteAsync_Should_PreserveResultOrder_When_MultipleVariableSetsAreRead(
        bool variableBatching,
        bool stopEarly)
    {
        // arrange
        await using var fixture = await BatchBufferTestFixture.CreateAsync();
        var events = new List<string>();
        var payload = variableBatching
            ? """[{"data":{"field":"a"},"variableIndex":1},{"data":{"field":"b"},"variableIndex":0}]"""
            : """[{"data":{"field":"a"}},{"data":{"field":"b"}}]""";
        using var graphQLClient = new DefaultGraphQLHttpClient(
            new HttpClient(new ResponseHandler(new TrackingContent(payload, events))),
            disposeInnerClient: true);
        await using var client = new HttpSourceSchemaClient(graphQLClient,
            new HttpSourceSchemaClientConfiguration("A", new Uri("http://localhost/graphql"),
                capabilities: variableBatching
                    ? SourceSchemaClientCapabilities.VariableBatching
                    : SourceSchemaClientCapabilities.RequestBatching,
                onSourceSchemaResult: (_, _, result) =>
                    events.Add($"callback:{result.Data.GetProperty("field").GetString()}")));
        var firstPath = new CompactPath([1, 2]);
        var secondPath = new CompactPath([1, 3]);
        var request = CreateRequest(fixture.RootNode) with
        {
            Variables = [
                new VariableValues(firstPath, JsonSegment.Empty),
                new VariableValues(secondPath, JsonSegment.Empty)]
        };

        // act
        var resultCount = 0;
        await foreach (var result in client.ExecuteAsync(
            fixture.CreateContext(), request, TestContext.Current.CancellationToken))
        {
            var expectedPath = (variableBatching, resultCount) switch
            {
                (true, 0) or (false, 1) => secondPath,
                _ => firstPath
            };
            events.Add($"consume:{result.Data.GetProperty("field").GetString()}:"
                + (result.Path == expectedPath));
            result.Dispose();
            resultCount++;
            if (stopEarly)
            {
                break;
            }
        }

        // assert
        if (stopEarly)
        {
            events.MatchInlineSnapshot(
                """
                [
                  "callback:a",
                  "consume:a:True",
                  "disposed"
                ]
                """);
        }
        else
        {
            events.MatchInlineSnapshot(
                """
                [
                  "callback:a",
                  "consume:a:True",
                  "callback:b",
                  "consume:b:True",
                  "disposed"
                ]
                """);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExecuteAsync_Should_ObserveCancellation_When_EitherEnumerationTokenIsCanceled(
        bool cancelExecuteToken,
        bool callbackEnabled)
    {
        // arrange
        await using var fixture = await BatchBufferTestFixture.CreateAsync();
        using var executeCancellation = new CancellationTokenSource();
        using var enumerationCancellation = new CancellationTokenSource();
        var events = new List<string>();
        using var graphQLClient = new DefaultGraphQLHttpClient(
            new HttpClient(new ResponseHandler(new TrackingContent("""{"data":null}""", events))),
            disposeInnerClient: true);
        await using var client = new HttpSourceSchemaClient(graphQLClient,
            new HttpSourceSchemaClientConfiguration("A", new Uri("http://localhost/graphql"),
                onSourceSchemaResult: callbackEnabled ? (_, _, _) => events.Add("callback") : null));
        await using var enumerator = client.ExecuteAsync(
            fixture.CreateContext(), CreateRequest(fixture.RootNode), executeCancellation.Token)
            .GetAsyncEnumerator(enumerationCancellation.Token);
        (cancelExecuteToken ? executeCancellation : enumerationCancellation).Cancel();

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => enumerator.MoveNextAsync().AsTask());

        // assert
        events.MatchInlineSnapshot(
            """
            [
              "disposed"
            ]
            """);
    }

    [Fact]
    public async Task ExecuteBatchStreamAsync_Should_DisposeRequestBuffer_When_StreamIsConsumed()
    {
        // arrange
        await using var fixture = await BatchBufferTestFixture.CreateAsync();
        using var graphQLClient = new DefaultGraphQLHttpClient(
            new HttpClient(new BatchHandler()),
            disposeInnerClient: true);
        await using var client = new HttpSourceSchemaClient(
            graphQLClient,
            new HttpSourceSchemaClientConfiguration("A", new Uri("http://localhost:5000/graphql")));
        var context = fixture.CreateContext();
        using var requestBuffer = new ChunkedArrayWriter();
        var requests = ImmutableArray.Create(
            CreateRequest(fixture.RootNode),
            CreateRequest(fixture.RootNode));
        var httpRequest = new GraphQLHttpRequest(CreateBatchRequest())
        {
            Uri = new Uri("http://localhost:5000/graphql"),
            AcceptHeaderValue = "application/json",
            OperationKind = OperationType.Query
        };

        // act
        var resultCount = 0;
        await foreach (var batchResult in client.ExecuteBatchStreamAsync(
            context,
            requests,
            httpRequest,
            requestBuffer,
            TestContext.Current.CancellationToken))
        {
            batchResult.Result.Dispose();
            resultCount++;
        }

        // assert
        Assert.Equal(2, resultCount);
        Assert.Throws<ObjectDisposedException>(DrainDisposedBuffer);

        void DrainDisposedBuffer()
        {
            var (chunks, usedChunks, _) = requestBuffer.DrainChunks();

            if (usedChunks > 0)
            {
                JsonMemory.Return(JsonMemoryKind.Variables, chunks, usedChunks);
            }

            ArrayPool<byte[]>.Shared.Return(chunks, clearArray: true);
        }
    }

    [Fact]
    public async Task ExecuteBatchAsync_Should_IsolateFailure_When_OneRequestFailsMidSequence()
    {
        // arrange
        // Capabilities without request batching force the fallback that sends
        // each request as its own HTTP round-trip, so a single failure must not
        // abort the batch.
        await using var fixture = await BatchBufferTestFixture.CreateAsync();
        using var graphQLClient = new DefaultGraphQLHttpClient(
            new HttpClient(new SelectiveFailureHandler()),
            disposeInnerClient: true);
        await using var client = new HttpSourceSchemaClient(
            graphQLClient,
            new HttpSourceSchemaClientConfiguration(
                "A",
                new Uri("http://localhost:5000/graphql"),
                capabilities: SourceSchemaClientCapabilities.None));
        var context = fixture.CreateContext();
        var requests = ImmutableArray.Create(
            CreateRequest(fixture.RootNode),
            CreateRequest(fixture.RootNode),
            CreateRequest(fixture.RootNode));

        // act
        var produced = new List<string>();
        await foreach (var batchResult in client.ExecuteBatchAsync(
            context,
            requests,
            TestContext.Current.CancellationToken))
        {
            produced.Add($"{batchResult.RequestIndex}:{batchResult.Result.Data.GetProperty("field").GetString()}");
            batchResult.Result.Dispose();
        }

        // assert
        // The second request fails in transport; the siblings still produce their
        // data and exactly one cause is recorded against the failing request index.
        Assert.Equal(new[] { "0:a", "2:c" }, produced);
        Assert.True(context.TryGetBatchRequestError(fixture.RootNode, 1, out var recordedError));
        Assert.IsType<HttpRequestException>(recordedError);
    }

    private static SourceSchemaClientRequest CreateRequest(ExecutionNode node)
    {
        var sourceText = "query { field }"u8.ToArray();

        return new SourceSchemaClientRequest
        {
            Node = node,
            SchemaName = "A",
            OperationType = OperationType.Query,
            OperationSourceText = new OperationSourceText(
                "Op",
                OperationType.Query,
                sourceText,
                OperationSourceTextHash.Compute(sourceText)),
            OperationDocument = Utf8GraphQLOperationParser.Parse(sourceText),
            Variables = [new VariableValues(CompactPath.Root, JsonSegment.Empty)]
        };
    }

    private static OperationBatchRequest CreateBatchRequest()
    {
        return new OperationBatchRequest(
        [
            new FusionOperationRequest(
                "query { field }"u8.ToArray(),
                id: null,
                operationName: null,
                onError: null,
                VariableValues.Empty,
                JsonSegment.Empty),
            new FusionOperationRequest(
                "query { field }"u8.ToArray(),
                id: null,
                operationName: null,
                onError: null,
                VariableValues.Empty,
                JsonSegment.Empty)
        ]);
    }

    private sealed class BatchBufferTestFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly IRequestExecutor _executor;
        private readonly OperationPlan _operationPlan;
        private readonly List<OperationPlanContext> _rentedContexts = [];
        private readonly List<CancellationTokenSource> _ctsList = [];
        private readonly List<(ObjectPool<PooledRequestContext> Pool, PooledRequestContext Context)>
            _requestContexts = [];

        private BatchBufferTestFixture(
            ServiceProvider services,
            IRequestExecutor executor,
            OperationPlan operationPlan)
        {
            _services = services;
            _executor = executor;
            _operationPlan = operationPlan;
        }

        public ExecutionNode RootNode => _operationPlan.RootNodes[0];

        public static async Task<BatchBufferTestFixture> CreateAsync()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            var services = serviceCollection
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(
                    ComposeSchemaDocument(
                        """
                        type Query {
                          field: String!
                        }
                        """))
                .ModifyRequestOptions(options => options.CollectOperationPlanTelemetry = true)
                .Services
                .BuildServiceProvider();

            var executor = await services.GetRequestExecutorAsync();
            var schema = (Fusion.Types.FusionSchemaDefinition)executor.Schema;
            var operationPlan = PlanOperation(
                schema,
                """
                query {
                  field
                }
                """);

            return new BatchBufferTestFixture(services, executor, operationPlan);
        }

        public OperationPlanContext CreateContext()
        {
            var contextPool = _executor.Schema.Services.GetRequiredService<OperationPlanContextPool>();
            var context = contextPool.Rent();
            var cts = new CancellationTokenSource();
            var requestContextPool =
                _executor.Schema.Services.GetRequiredService<ObjectPool<PooledRequestContext>>();
            var requestContext = requestContextPool.Get();
            var request = OperationRequestBuilder.New()
                .SetDocument("{ field }")
                .Build();

            requestContext.Initialize(
                _executor.Schema,
                _executor.Version,
                request,
                requestIndex: 0,
                requestServices: _services,
                requestAborted: CancellationToken.None);

            context.Initialize(requestContext, VariableValueCollection.Empty, _operationPlan, cts, new MemoryArena());

            _ctsList.Add(cts);
            _requestContexts.Add((requestContextPool, requestContext));
            _rentedContexts.Add(context);
            return context;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var context in _rentedContexts)
            {
                await context.DisposeAsync();
            }

            foreach (var (pool, context) in _requestContexts)
            {
                pool.Return(context);
            }

            foreach (var cts in _ctsList)
            {
                cts.Dispose();
            }

            await _services.DisposeAsync();
        }
    }

    private sealed class SelectiveFailureHandler : HttpMessageHandler
    {
        private int _callCount;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref _callCount);

            if (call == 2)
            {
                throw new HttpRequestException(
                    "The connection was reset while sending the second request.");
            }

            var field = call == 1 ? "a" : "c";
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"data\":{\"field\":\"" + field + "\"}}",
                    Encoding.UTF8,
                    "application/json")
            };

            return Task.FromResult(response);
        }
    }

    private sealed class AmbientTrackingContent(Action onDispose)
        : StringContent("""{"data":null}""", Encoding.UTF8, "application/json")
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                onDispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class ResponseHandler(HttpContent content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = content });
    }

    private sealed class TrackingContent(string content, List<string> events)
        : StringContent(content, Encoding.UTF8, "application/json")
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                events.Add("disposed");
            }

            base.Dispose(disposing);
        }
    }

    private sealed class BatchHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                      {
                        "data": {
                          "field": "a"
                        },
                        "requestIndex": 0
                      },
                      {
                        "data": {
                          "field": "b"
                        },
                        "requestIndex": 1
                      }
                    ]
                    """,
                    Encoding.UTF8,
                    "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
