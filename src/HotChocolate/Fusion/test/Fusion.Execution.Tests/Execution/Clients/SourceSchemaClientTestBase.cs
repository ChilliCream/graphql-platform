using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Clients;

public abstract class SourceSchemaClientTestBase : FusionTestBase
{
    protected sealed class SourceSchemaClientTestFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly IRequestExecutor _executor;
        private readonly OperationPlan _operationPlan;
        private readonly List<OperationPlanContext> _rentedContexts = [];
        private readonly List<CancellationTokenSource> _ctsList = [];
        private readonly List<(ObjectPool<PooledRequestContext> Pool, PooledRequestContext Context)>
            _requestContexts = [];

        private SourceSchemaClientTestFixture(
            ServiceProvider services,
            IRequestExecutor executor,
            OperationPlan operationPlan)
        {
            _services = services;
            _executor = executor;
            _operationPlan = operationPlan;
        }

        public ExecutionNode RootNode => _operationPlan.RootNodes[0];

        public static async Task<SourceSchemaClientTestFixture> CreateAsync()
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

            return new SourceSchemaClientTestFixture(services, executor, operationPlan);
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
}
