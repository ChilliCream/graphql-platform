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
        using var factory = new WebSocketSourceSchemaClientFactory();
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
    public async Task GetClient_Should_CreateDistinctCustomClients_When_WebSocketConfigurationIsUsed()
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
        Assert.NotSame(queryClient, subscriptionClient);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetClient_Should_CreateDistinctWebSocketClients_When_ConfigurationsDiffer(
        bool subscriptionRequestedFirst)
    {
        // arrange
        using var factory = new WebSocketSourceSchemaClientFactory();
        await using var scope = CreateScope(
            factory,
            new WebSocketSourceSchemaClientConfiguration(
                "A",
                new Uri("ws://localhost/query"),
                SupportedOperationType.Query),
            new WebSocketSourceSchemaClientConfiguration(
                "A",
                new Uri("ws://localhost/subscription"),
                SupportedOperationType.Subscription));

        ISourceSchemaClient queryClient;
        ISourceSchemaClient subscriptionClient;

        // act
        if (subscriptionRequestedFirst)
        {
            subscriptionClient = scope.GetClient("A", OperationType.Subscription);
            queryClient = scope.GetClient("A", OperationType.Query);
        }
        else
        {
            queryClient = scope.GetClient("A", OperationType.Query);
            subscriptionClient = scope.GetClient("A", OperationType.Subscription);
        }

        // assert
        Assert.NotSame(queryClient, subscriptionClient);
    }

    private static DefaultSourceSchemaClientScope CreateScope(
        ISourceSchemaClientFactory factory,
        params ISourceSchemaClientConfiguration[] configurations)
    {
        var features = new FeatureCollection();
        features.Set(new SourceSchemaClientConfigurations(configurations));

        var schema = FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse("enum fusion__Schema { A } type Query { foo: String }"),
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
