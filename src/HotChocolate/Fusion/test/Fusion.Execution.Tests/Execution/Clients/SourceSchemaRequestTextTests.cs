using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaRequestTextTests : FusionTestBase
{
    [Fact]
    public async Task ExecuteAsync_Should_SendCompactText_When_PlanIsFormattedAsYaml()
    {
        // arrange
        await using var fixture = await K6Fixture.CreateAsync();
        var node = fixture.RootNode;
        var handler = new CapturingHandler();
        using var graphQLClient = new DefaultGraphQLHttpClient(
            new HttpClient(handler),
            disposeInnerClient: true);
        await using var client = new HttpSourceSchemaClient(
            graphQLClient,
            new HttpSourceSchemaClientConfiguration(node.SchemaName!, new Uri("http://localhost:5000/graphql")));
        var context = fixture.CreateContext();
        var request = new SourceSchemaClientRequest
        {
            Node = node,
            SchemaName = node.SchemaName!,
            OperationType = node.Operation.Type,
            OperationSourceText = node.Operation,
            OperationDocument = node.OperationDocument
        };

        // act
        await foreach (var result in client.ExecuteAsync(context, request, TestContext.Current.CancellationToken))
        {
            result.Dispose();
        }

        // assert
        using var body = JsonDocument.Parse(handler.Body);
        var sentQuery = body.RootElement.GetProperty("query").GetString();
        Assert.Equal(Encoding.UTF8.GetString(node.Operation.Value.Span), sentQuery);
        Assert.Equal(OperationSourceTextHash.Compute(node.Operation.Value.Span), node.Operation.Hash);
        fixture.RootNodeYaml.MatchInlineSnapshot(
            """
            - id: 1
              type: Operation
              schema: c
              operation: |
                query TestQuery_123456789101112_1 {
                  topProducts(first: 5) {
                    name
                    price
                    upc
                    weight
                  }
                }
            """);
        handler.Body.MatchInlineSnapshot(
            "{\"query\":\"query TestQuery_123456789101112_1 { topProducts(first: 5) { name price upc weight } }\"}");
    }

    private sealed class K6Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly IRequestExecutor _executor;
        private readonly OperationPlan _operationPlan;
        private readonly List<OperationPlanContext> _rentedContexts = [];
        private readonly List<CancellationTokenSource> _ctsList = [];
        private readonly List<(ObjectPool<PooledRequestContext> Pool, PooledRequestContext Context)>
            _requestContexts = [];

        private K6Fixture(
            ServiceProvider services,
            IRequestExecutor executor,
            OperationPlan operationPlan)
        {
            _services = services;
            _executor = executor;
            _operationPlan = operationPlan;
        }

        public OperationExecutionNode RootNode
            => _operationPlan.RootNodes.OfType<OperationExecutionNode>().First();

        public string RootNodeYaml
        {
            get
            {
                var yaml = new YamlOperationPlanFormatter().Format(_operationPlan);
                var nodes = yaml[(yaml.IndexOf("nodes:\n", StringComparison.Ordinal) + 7)..];
                var end = nodes.IndexOf("  - id:", 1, StringComparison.Ordinal);
                return string.Join('\n', nodes[..end].TrimEnd().Split('\n').Select(t => t[2..]));
            }
        }

        public static async Task<K6Fixture> CreateAsync()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient();
            var services = serviceCollection
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(
                    ComposeSchemaDocument(
                        FileResource.Open("k6-accounts.graphqls"),
                        FileResource.Open("k6-inventory.graphqls"),
                        FileResource.Open("k6-products.graphqls"),
                        FileResource.Open("k6-reviews.graphqls")))
                .Services
                .BuildServiceProvider();

            var executor = await services.GetRequestExecutorAsync();
            var schema = (FusionSchemaDefinition)executor.Schema;
            var operationPlan = PlanOperation(schema, FileResource.Open("k6.graphql"));

            return new K6Fixture(services, executor, operationPlan);
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
                .SetDocument(FileResource.Open("k6.graphql"))
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

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "data": {}
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
