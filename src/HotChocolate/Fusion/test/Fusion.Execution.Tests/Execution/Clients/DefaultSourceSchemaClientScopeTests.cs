using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class DefaultSourceSchemaClientScopeTests
{
    [Fact]
    public async Task GetClient_Should_ReuseWebSocketClient_When_OperationTypesDiffer()
    {
        // arrange
        var factory = new TrackingClientFactory();
        await using var scope = CreateScope(
            factory,
            new WebSocketSourceSchemaClientConfiguration("A", new Uri("ws://localhost/graphql")));

        // act
        var queryClient = scope.GetClient("A", OperationType.Query);
        var subscriptionClient = scope.GetClient("A", OperationType.Subscription);

        // assert
        Assert.Same(queryClient, subscriptionClient);
    }

    [Fact]
    public async Task GetClient_Should_CreateDistinctHttpClients_When_OperationTypesDiffer()
    {
        // arrange
        var factory = new TrackingClientFactory();
        await using var scope = CreateScope(
            factory,
            new HttpSourceSchemaClientConfiguration("A", new Uri("http://localhost/graphql")));

        // act
        var queryClient = scope.GetClient("A", OperationType.Query);
        var subscriptionClient = scope.GetClient("A", OperationType.Subscription);

        // assert
        Assert.NotSame(queryClient, subscriptionClient);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeWebSocketClientOnce_When_UsedForMultipleOperationTypes()
    {
        // arrange
        var factory = new TrackingClientFactory();
        var scope = CreateScope(
            factory,
            new WebSocketSourceSchemaClientConfiguration("A", new Uri("ws://localhost/graphql")));

        _ = scope.GetClient("A", OperationType.Query);
        _ = scope.GetClient("A", OperationType.Subscription);

        // act
        await scope.DisposeAsync();

        // assert
        var client = Assert.Single(factory.Clients);
        Assert.Equal(1, client.DisposeCount);
    }

    private static DefaultSourceSchemaClientScope CreateScope(
        ISourceSchemaClientFactory factory,
        ISourceSchemaClientConfiguration configuration)
    {
        var features = new FeatureCollection();
        features.Set(new SourceSchemaClientConfigurations([configuration]));

        var schema = FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse("type Query { foo: String }"),
            features: features);

        return new DefaultSourceSchemaClientScope(schema, [factory]);
    }

    private sealed class TrackingClientFactory : ISourceSchemaClientFactory
    {
        public List<TrackingClient> Clients { get; } = [];

        public bool CanHandle(ISourceSchemaClientConfiguration configuration) => true;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
        {
            var client = new TrackingClient();
            Clients.Add(client);
            return client;
        }
    }

    private sealed class TrackingClient : ISourceSchemaClient
    {
        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public int DisposeCount { get; private set; }

        public IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

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

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}
