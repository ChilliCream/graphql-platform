using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using FusionIOperationRequest = HotChocolate.Fusion.Transport.IOperationRequest;
using FusionOperationRequest = HotChocolate.Fusion.Transport.OperationRequest;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class BatchBufferTests : FusionTestBase
{
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

    private static SourceSchemaClientRequest CreateRequest(ExecutionNode node)
    {
        return new SourceSchemaClientRequest
        {
            Node = node,
            SchemaName = "A",
            OperationType = OperationType.Query,
            OperationSourceText = "query { field }",
            OperationHash = 1,
            Variables = [new VariableValues(CompactPath.Root, JsonSegment.Empty)]
        };
    }

    private static OperationBatchRequest CreateBatchRequest()
    {
        return new OperationBatchRequest(
            ImmutableArray.Create<FusionIOperationRequest>(
                new FusionOperationRequest(
                    "query { field }",
                    id: null,
                    operationName: null,
                    onError: null,
                    VariableValues.Empty,
                    JsonSegment.Empty),
                new FusionOperationRequest(
                    "query { field }",
                    id: null,
                    operationName: null,
                    onError: null,
                    VariableValues.Empty,
                    JsonSegment.Empty)));
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
