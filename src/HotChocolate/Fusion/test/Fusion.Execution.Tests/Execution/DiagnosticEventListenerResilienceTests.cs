using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class DiagnosticEventListenerResilienceTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_FetchAndReturnData_When_ListenerThrowsOnScopeCreation()
    {
        // arrange
        var client = new RecordingQueryClient();
        var listener = new ThrowingScopeListener(throwOnScopeCreation: true);
        var executor = await CreateExecutorAsync(client, listener);
        var request = CreateQueryRequest();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Equal(1, client.ExecutionCount);
        operationResult.MatchInlineSnapshot(
            """
            {
              "data": {
                "field": "hello"
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_CompleteAndReturnData_When_ListenerScopeThrowsOnDispose()
    {
        // arrange
        var client = new RecordingQueryClient();
        var listener = new ThrowingScopeListener(throwOnScopeCreation: false);
        var executor = await CreateExecutorAsync(client, listener);
        var request = CreateQueryRequest();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Equal(1, client.ExecutionCount);
        Assert.True(listener.ScopeDisposed);
        operationResult.MatchInlineSnapshot(
            """
            {
              "data": {
                "field": "hello"
              }
            }
            """);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        ISourceSchemaClient client,
        IFusionExecutionDiagnosticEventListener listener)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .AddDiagnosticEventListener(_ => listener)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    # name: a
                    type Query {
                      field: String
                    }
                    """));

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestClientFactory(client));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static IOperationRequest CreateQueryRequest()
        => OperationRequestBuilder.New()
            .SetDocument(
                """
                query {
                  field
                }
                """)
            .Build();

    private sealed class ThrowingScopeListener(bool throwOnScopeCreation)
        : FusionExecutionDiagnosticEventListener
    {
        public bool ScopeDisposed { get; private set; }

        public override IDisposable ExecuteOperationNode(
            OperationPlanContext context,
            OperationExecutionNode node,
            string schemaName)
            => throwOnScopeCreation
                ? throw new InvalidOperationException("The diagnostic scope could not be created.")
                : new ThrowingScope(this);

        private sealed class ThrowingScope(ThrowingScopeListener listener) : IDisposable
        {
            public void Dispose()
            {
                listener.ScopeDisposed = true;
                throw new InvalidOperationException("The diagnostic scope could not be disposed.");
            }
        }
    }

    private sealed class RecordingQueryClient : ISourceSchemaClient
    {
        private static readonly byte[] s_payload = """{"data":{"field":"hello"}}"""u8.ToArray();
        private int _executionCount;

        public int ExecutionCount => _executionCount;

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _executionCount);

            await Task.Yield();
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, s_payload, s_payload.Length);

            yield return new SourceSchemaResult(CompactPath.Root, document);
        }

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TestClientFactory(ISourceSchemaClient client)
        : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is TestClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => client;
    }

    private sealed class TestClientConfiguration(string name)
        : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.Query;
    }
}
