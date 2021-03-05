using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Snapshooter.Xunit;
using StrawberryShake.Transport.WebSockets.Protocols;
using Xunit;

namespace StrawberryShake.Transport.InMemory
{
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
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddGraphQLServer()
                .AddStarWarsTypes()
                .AddExportDirectiveType()
                .AddStarWarsRepositories()
                .AddInMemorySubscriptions();

            serviceCollection
                .AddInMemoryClient("Foo");

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            List<JsonDocument> results = new();
            MockDocument document = new("query Foo { hero { name } }");
            OperationRequest request = new("Foo", document);

            IInMemoryClientFactory factory = services
                .GetRequiredService<IInMemoryClientFactory>();

            // act
            var connection =
                new InMemoryConnection(async ct => await factory.CreateClientAsync("Foo", ct));

            await foreach (var response in connection.ExecuteAsync(request, ct))
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
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddGraphQLServer("Foo")
                .AddStarWarsTypes()
                .AddExportDirectiveType()
                .AddStarWarsRepositories()
                .AddInMemorySubscriptions();

            serviceCollection
                .AddProtocol<GraphQLWebSocketProtocolFactory>()
                .AddInMemoryClient("Foo")
                .ConfigureInMemoryClient(x => x.SchemaName = "Foo");

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            List<JsonDocument> results = new();
            MockDocument document = new("query Foo { hero { name } }");
            OperationRequest request = new("Foo", document);

            IInMemoryClientFactory factory = services
                .GetRequiredService<IInMemoryClientFactory>();

            // act
            var connection =
                new InMemoryConnection(async ct => await factory.CreateClientAsync("Foo", ct));

            await foreach (var response in connection.ExecuteAsync(request, ct))
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
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            var serviceCollection = new ServiceCollection();

            string? result = null!;
            serviceCollection
                .AddGraphQLServer()
                .AddStarWarsTypes()
                .AddExportDirectiveType()
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

            List<JsonDocument> results = new();
            MockDocument document = new("query Foo { hero { name } }");
            OperationRequest request = new("Foo", document);

            IInMemoryClientFactory factory = services
                .GetRequiredService<IInMemoryClientFactory>();

            // act
            var connection =
                new InMemoryConnection(async ct => await factory.CreateClientAsync("Foo", ct));

            await foreach (var response in connection.ExecuteAsync(request, ct))
            {
                if (response.Body is not null)
                {
                    results.Add(response.Body);
                }
            }

            // assert
            Assert.Equal("bar", result);
        }

        [Fact]
        public async Task Subscription_Result()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddGraphQLServer()
                .AddStarWarsTypes()
                .AddTypeExtension<StringSubscriptionExtensions>()
                .AddExportDirectiveType()
                .AddStarWarsRepositories()
                .AddInMemorySubscriptions();

            serviceCollection
                .AddProtocol<GraphQLWebSocketProtocolFactory>()
                .AddInMemoryClient("Foo");

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            List<JsonDocument> results = new();
            MockDocument document = new("subscription Test { onTest(id:1) }");
            OperationRequest request = new("Test", document);

            IInMemoryClientFactory factory = services
                .GetRequiredService<IInMemoryClientFactory>();

            // act
            var connection =
                new InMemoryConnection(async ct => await factory.CreateClientAsync("Foo", ct));

            await foreach (var response in connection.ExecuteAsync(request, ct))
            {
                if (response.Body is not null)
                {
                    results.Add(response.Body);
                }
            }

            // assert
            results.Select(x => x.RootElement.ToString()).ToList().MatchSnapshot();
        }

        private class MockDocument : IDocument
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
                IQueryRequestBuilder requestBuilder,
                CancellationToken cancellationToken)
            {
                requestBuilder.AddProperty("Foo", "bar");
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
                    yield return $"{id.Value}num{i}";
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
}
