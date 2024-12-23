using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets.Protocols;

#pragma warning disable CS0618

namespace StrawberryShake.Transport.InMemory;

public class IntegrationTests : ServerTestBase
{
    public IntegrationTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Simple_Request()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddGraphQLServer()
            .AddStarWarsTypes()
            .AddStarWarsRepositories()
            .AddInMemorySubscriptions();

        serviceCollection
            .AddInMemoryClient("Foo");

        IServiceProvider services =
            serviceCollection.BuildServiceProvider();

        List<JsonDocument> results = [];
        MockDocument document = new("query Foo { hero { name } }");
        OperationRequest request = new("Foo", document);

        var factory = services
            .GetRequiredService<IInMemoryClientFactory>();

        // act
        var connection = new InMemoryConnection(async abort => await factory.CreateAsync("Foo", abort));

        await foreach (var response in
            connection.ExecuteAsync(request).WithCancellation(ct))
        {
            if (response.Body is not null)
            {
                results.Add(response.Body);
            }
        }

        // assert
        results.Select(x => x.RootElement.ToString()).ToList().MatchSnapshot();
    }

    [Fact]
    public async Task Configure_SchemaName()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddGraphQLServer("Foo")
            .AddStarWarsTypes()
            .AddStarWarsRepositories()
            .AddInMemorySubscriptions();

        serviceCollection
            .AddProtocol<GraphQLWebSocketProtocolFactory>()
            .AddInMemoryClient("Foo")
            .ConfigureInMemoryClient(x => x.SchemaName = "Foo");

        IServiceProvider services =
            serviceCollection.BuildServiceProvider();

        List<JsonDocument> results = [];
        MockDocument document = new("query Foo { hero { name } }");
        OperationRequest request = new("Foo", document);

        var factory = services
            .GetRequiredService<IInMemoryClientFactory>();

        // act
        var connection =
            new InMemoryConnection(async abort => await factory.CreateAsync("Foo", abort));

        await foreach (var response in
            connection.ExecuteAsync(request).WithCancellation(ct))
        {
            if (response.Body is not null)
            {
                results.Add(response.Body);
            }
        }

        // assert
        results.Select(x => x.RootElement.ToString()).ToList().MatchSnapshot();
    }

    [Fact]
    public async Task Interceptor_Set_ContextData()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        var serviceCollection = new ServiceCollection();

        string? result = null!;
        serviceCollection
            .AddGraphQLServer()
            .AddStarWarsTypes()
            .AddStarWarsRepositories()
            .AddInMemorySubscriptions()
            .UseField(next => context =>
            {
                result = context.ContextData["Foo"] as string;
                return next(context);
            });

        serviceCollection
            .AddProtocol<GraphQLWebSocketProtocolFactory>()
            .AddInMemoryClient("Foo")
            .ConfigureRequestInterceptor(new StubInterceptor());

        IServiceProvider services =
            serviceCollection.BuildServiceProvider();

        List<string> results = [];
        MockDocument document = new("query Foo { hero { name } }");
        OperationRequest request = new("Foo", document);

        var factory = services
            .GetRequiredService<IInMemoryClientFactory>();

        // act
        var connection =
            new InMemoryConnection(async abort => await factory.CreateAsync("Foo", abort));

        await foreach (var response in
            connection.ExecuteAsync(request).WithCancellation(ct))
        {
            if (response.Body is not null)
            {
                results.Add(response.Body.RootElement.ToString());
            }
        }

        // assert
        Assert.Equal("bar", result);
        results.MatchSnapshot();
    }

    [Fact]
    public async Task Subscription_Result()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddGraphQLServer()
            .AddStarWarsTypes()
            .AddTypeExtension<StringSubscriptionExtensions>()
            .AddStarWarsRepositories()
            .AddInMemorySubscriptions();

        serviceCollection
            .AddProtocol<GraphQLWebSocketProtocolFactory>()
            .AddInMemoryClient("Foo");

        IServiceProvider services =
            serviceCollection.BuildServiceProvider();

        List<string> results = [];
        MockDocument document = new("subscription Test { onTest(id:1) }");
        OperationRequest request = new("Test", document);

        var factory = services
            .GetRequiredService<IInMemoryClientFactory>();

        // act
        var connection = new InMemoryConnection(
            async abort => await factory.CreateAsync("Foo", abort));

        await foreach (var response in
            connection.ExecuteAsync(request).WithCancellation(ct))
        {
            if (response.Body is not null)
            {
                results.Add(response.Body.RootElement.ToString());
            }
        }

        // assert
        results.MatchSnapshot();
    }

    private sealed class MockDocument : IDocument
    {
        private readonly byte[] _query;

        public MockDocument(string query)
        {
            _query = Encoding.UTF8.GetBytes(query);
        }

        public OperationKind Kind => OperationKind.Query;

        public ReadOnlySpan<byte> Body => _query;

        public DocumentHash Hash { get; } = new("MD5", "ABC");
    }

    public class StubInterceptor : IInMemoryRequestInterceptor
    {
        public ValueTask OnCreateAsync(
            IServiceProvider serviceProvider,
            OperationRequest request,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            requestBuilder.AddGlobalState("Foo", "bar");
            return default;
        }
    }

    [ExtendObjectType("Subscription")]
    public class StringSubscriptionExtensions
    {
        [SubscribeAndResolve]
        public async IAsyncEnumerable<string> OnTest(int? id)
        {
            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(1);
                yield return $"{id}num{i}";
            }
        }

        [SubscribeAndResolve]
        public async IAsyncEnumerable<int> CountUp()
        {
            for (var i = 0; i < 100; i++)
            {
                await Task.Delay(1);
                yield return i;
            }
        }
    }
}
