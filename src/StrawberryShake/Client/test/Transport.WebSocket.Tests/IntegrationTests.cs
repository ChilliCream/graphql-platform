using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Types;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.Http.Subscriptions;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
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
            using IWebHost host = TestServerHelper.CreateServer(
                x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                out int port);
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddProtocol<GraphQLWebSocketProtocolFactory>()
                .AddWebSocketClient(
                    "Foo",
                    c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            ISocketClientPool connectionPool =
                services.GetRequiredService<ISocketClientPool>();

            List<JsonDocument> results = new();
            MockDocument document = new("subscription Test { onTest(id:1) }");
            OperationRequest request = new("Test", document);

            // act
            ISocketClient socketClient = await connectionPool.RentAsync("Foo");

            try
            {
                await using var manager = new SocketOperationManager(socketClient);
                var connection = new WebSocketConnection(manager);
                await foreach (var response in connection
                    .ExecuteAsync(request, new CancellationTokenSource(1000_000).Token))
                {
                    if (response.Body is not null)
                    {
                        results.Add(response.Body);
                    }

                    if (results.Count == 10)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await connectionPool.ReturnAsync(socketClient);
            }


            // assert
            results.Select(x => x.RootElement.ToString()).ToList().MatchSnapshot();
        }

        [Fact]
        public async Task Execution_Error()
        {
            // arrange
            using IWebHost host = TestServerHelper.CreateServer(
                x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                out int port);
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddProtocol<GraphQLWebSocketProtocolFactory>()
                .AddWebSocketClient(
                    "Foo",
                    c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            ISocketClientPool connectionPool =
                services.GetRequiredService<ISocketClientPool>();

            List<JsonDocument> results = new();
            MockDocument document = new("subscription Test { onTest }");
            OperationRequest request = new("Test", document);

            // act
            ISocketClient socketClient = await connectionPool.RentAsync("Foo");

            try
            {
                await using var manager = new SocketOperationManager(socketClient);
                var connection = new WebSocketConnection(manager);
                await foreach (var response in connection
                    .ExecuteAsync(request, new CancellationTokenSource(1000_000).Token))
                {
                    if (response.Body is not null)
                    {
                        results.Add(response.Body);
                    }

                    if (results.Count == 10)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await connectionPool.ReturnAsync(socketClient);
            }


            // assert
            results.Select(x => x.RootElement.ToString()).ToList().MatchSnapshot();
        }

        [Fact]
        public async Task Validation_Error()
        {
            // arrange
            using IWebHost host = TestServerHelper.CreateServer(
                x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                out int port);
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddProtocol<GraphQLWebSocketProtocolFactory>()
                .AddWebSocketClient(
                    "Foo",
                    c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            ISocketClientPool connectionPool =
                services.GetRequiredService<ISocketClientPool>();

            List<JsonDocument> results = new();
            MockDocument document = new(@"subscription Test { onTest(id:""Foo"") }");
            OperationRequest request = new("Test", document);

            // act
            ISocketClient socketClient = await connectionPool.RentAsync("Foo");

            try
            {
                await using var manager = new SocketOperationManager(socketClient);
                var connection = new WebSocketConnection(manager);
                await foreach (var response in connection
                    .ExecuteAsync(request, new CancellationTokenSource(1000_000).Token))
                {
                    if (response.Body is not null)
                    {
                        results.Add(response.Body);
                    }

                    if (results.Count == 10)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await connectionPool.ReturnAsync(socketClient);
            }


            // assert
            results.Select(x => x.RootElement.ToString()).ToList().MatchSnapshot();
        }

        [Fact]
        public async Task Parallel_Request()
        {
            // arrange
            using IWebHost host = TestServerHelper
                .CreateServer(
                    x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                    out int port);

            ServiceCollection serviceCollection = new();
            serviceCollection
                .AddProtocol<GraphQLWebSocketProtocolFactory>()
                .AddWebSocketClient(
                    "Foo",
                    c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            IServiceProvider services = serviceCollection.BuildServiceProvider();

            ISocketClientPool connectionPool = services.GetRequiredService<ISocketClientPool>();
            ConcurrentDictionary<int, List<JsonDocument>> results = new();


            // act
            ISocketClient socketClient = await connectionPool.RentAsync("Foo");
            await using var manager = new SocketOperationManager(socketClient);
            var connection = new WebSocketConnection(manager);

            try
            {
                async Task? CreateSubscription(int id)
                {
                    var document =
                        new MockDocument($"subscription Test {{ onTest(id:{id.ToString()}) }}");
                    var request = new OperationRequest("Test", document);
                    await foreach (var response in connection.ExecuteAsync(request))
                    {
                        if (response.Body is not null)
                        {
                            results.AddOrUpdate(id,
                                _ => new List<JsonDocument> { response.Body },
                                (_, l) =>
                                {
                                    l.Add(response.Body);
                                    return l;
                                });
                        }

                        if (results[id].Count == 10)
                        {
                            break;
                        }
                    }
                }

                var list = new List<Task>();
                for (var i = 0; i < 50; i++)
                {
                    list.Add(CreateSubscription(i));
                }

                await Task.WhenAll(list);
            }
            finally
            {
                await connectionPool.ReturnAsync(socketClient);
            }

            // assert
            results.Values
                .SelectMany(x => x.Select(x => x.RootElement.ToString()).ToList())
                .Aggregate((acc, res) => acc + "\n" + res)
                .MatchSnapshot();
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
        }
    }
}
