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
using StrawberryShake.Transport.WebSockets.Protocol;
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
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
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

            ISessionPool sessionPool =
                services.GetRequiredService<ISessionPool>();

            List<JsonDocument> results = new();
            MockDocument document = new("subscription Test { onTest(id:1) }");
            OperationRequest request = new("Test", document);

            // act
            var connection = new WebSocketConnection(() => sessionPool.CreateAsync("Foo", ct));
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
        public async Task Execution_Error()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
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

            ISessionPool sessionPool =
                services.GetRequiredService<ISessionPool>();

            List<JsonDocument> results = new();
            MockDocument document = new("subscription Test { onTest }");
            OperationRequest request = new("Test", document);

            // act
            var connection = new WebSocketConnection(() => sessionPool.CreateAsync("Foo", ct));
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
        public async Task Validation_Error()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(30_000).Token;
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

            ISessionPool sessionPool =
                services.GetRequiredService<ISessionPool>();

            List<JsonDocument> results = new();
            MockDocument document = new(@"subscription Test { onTest(id:""Foo"") }");
            OperationRequest request = new("Test", document);

            // act
            var connection = new WebSocketConnection(() => sessionPool.CreateAsync("Foo", ct));
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
        public async Task Parallel_Request_SameSocket()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
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

            ISessionPool sessionPool = services.GetRequiredService<ISessionPool>();
            ConcurrentDictionary<int, List<JsonDocument>> results = new();

            async Task? CreateSubscription(int id)
            {
                var connection =
                    new WebSocketConnection(() => sessionPool.CreateAsync("Foo", ct));
                var document =
                    new MockDocument($"subscription Test {{ onTest(id:{id.ToString()}) }}");
                var request = new OperationRequest("Test", document);
                await foreach (var response in connection.ExecuteAsync(request, ct))
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
                }
            }

            // act
            var list = new List<Task>();
            for (var i = 0; i < 15; i++)
            {
                list.Add(CreateSubscription(i));
            }

            await Task.WhenAll(list);

            // assert
            var str = "";
            foreach (var sub in results.OrderBy(x => x.Key))
            {
                JsonDocument[] jsonDocuments = sub.Value.ToArray();

                str += "Operation " + sub.Key + "\n";
                for (var index = 0; index < jsonDocuments.Length; index++)
                {
                    str += "Operation " + jsonDocuments[index].RootElement + "\n";
                }
            }

            str.MatchSnapshot();
        }

        [Fact]
        public async Task Parallel_Request_DifferentSockets()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            using IWebHost host = TestServerHelper
                .CreateServer(
                    x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                    out int port);

            ServiceCollection serviceCollection = new();
            serviceCollection
                .AddProtocol<GraphQLWebSocketProtocolFactory>();

            for (var i = 0; i < 10; i++)
            {
                serviceCollection.AddWebSocketClient(
                    "Foo" + i,
                    c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            }

            IServiceProvider services = serviceCollection.BuildServiceProvider();

            ISessionPool sessionPool = services.GetRequiredService<ISessionPool>();
            ConcurrentDictionary<int, List<JsonDocument>> results = new();

            async Task? CreateSubscription(int client, int id)
            {
                var connection =
                    new WebSocketConnection(() => sessionPool.CreateAsync("Foo" + client, ct));
                var document =
                    new MockDocument($"subscription Test {{ onTest(id:{id.ToString()}) }}");
                var request = new OperationRequest("Test", document);
                await foreach (var response in connection.ExecuteAsync(request, ct))
                {
                    if (response.Body is not null)
                    {
                        results.AddOrUpdate(client * 100 + id,
                            _ => new List<JsonDocument> { response.Body },
                            (_, l) =>
                            {
                                l.Add(response.Body);
                                return l;
                            });
                    }
                }
            }

            // act
            var list = new List<Task>();
            for (var i = 0; i < 5; i++)
            {
                for (var j = 0; j < 10; j++)
                {
                    list.Add(CreateSubscription(i, j));
                }
            }

            await Task.WhenAll(list);

            // assert
            var str = "";
            foreach (var sub in results.OrderBy(x => x.Key))
            {
                JsonDocument[] jsonDocuments = sub.Value.ToArray();

                str += "Operation " + sub.Key + "\n";
                for (var index = 0; index < jsonDocuments.Length; index++)
                {
                    str += "Operation " + jsonDocuments[index].RootElement + "\n";
                }
            }

            str.MatchSnapshot();
        }

        [Fact]
        public async Task LoadTest_MessagesReceivedInCorrectOrder()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            using IWebHost host = TestServerHelper
                .CreateServer(
                    x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                    out int port);

            ServiceCollection serviceCollection = new();
            serviceCollection
                .AddProtocol<GraphQLWebSocketProtocolFactory>();


            for (var i = 0; i < 10; i++)
            {
                serviceCollection.AddWebSocketClient(
                    "Foo" + i,
                    c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
            }

            IServiceProvider services = serviceCollection.BuildServiceProvider();

            ISessionPool sessionPool = services.GetRequiredService<ISessionPool>();

            var globalCounter = 0;

            async Task? CreateSubscription(int client, int id)
            {
                var connection =
                    new WebSocketConnection(() => sessionPool.CreateAsync("Foo" + client, ct));
                var document =
                    new MockDocument($"subscription Test {{ countUp }}");
                var request = new OperationRequest("Test", document);
                var counter = 0;
                await foreach (var response in connection.ExecuteAsync(request, ct))
                {
                    if (response.Body is not null)
                    {
                        Interlocked.Increment(ref globalCounter);
                        var received = response.Body.RootElement
                            .GetProperty("data")
                            .GetProperty("countUp")
                            .GetInt32();

                        if (counter != received)
                        {
                            throw new InvalidOperationException();
                        }

                        counter++;
                    }
                }
            }

            // act
            var list = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                for (var j = 0; j < 10; j++)
                {
                    list.Add(CreateSubscription(i, j));
                }
            }

            await Task.WhenAll(list);

            // assert
            Assert.Equal(10000, globalCounter);
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
