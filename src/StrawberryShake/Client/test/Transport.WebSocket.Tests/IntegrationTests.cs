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

                if (results.Count == 10)
                {
                    break;
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
        public async Task Parallel_Request()
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

                    if (results[id].Count == 10)
                    {
                        break;
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
            foreach (var sub in results)
            {
                JsonDocument[] jsonDocuments = sub.Value.ToArray();

                Assert.Equal(10, jsonDocuments.Length);

                for (var index = 0; index < jsonDocuments.Length; index++)
                {
                    JsonDocument? result = jsonDocuments[index];
                    var res = result.RootElement.GetProperty("data")
                        .GetProperty("onTest")
                        .GetString()
                        ?.Split("num");
                    str += res[0] + "num" + res[1];
                    Assert.Equal(sub.Key.ToString(), res?.FirstOrDefault());
                    Assert.Equal(index.ToString(), res?.LastOrDefault());
                }
            }

            str.MatchSnapshot();
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
