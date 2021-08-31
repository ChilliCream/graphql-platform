using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Messages;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Types;
using Snapshooter.Xunit;
using StrawberryShake.Transport.WebSockets.Protocols;
using Xunit;
using static HotChocolate.Tests.TestHelper;

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
            Snapshot.FullName();

            await TryTest(async ct =>
            {
                // arrange
                using IWebHost host = TestServerHelper.CreateServer(
                    x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                    out var port);
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
                var connection =
                    new WebSocketConnection(async _ => await sessionPool.CreateAsync("Foo", _));
                await foreach (var response in connection.ExecuteAsync(request, ct))
                {
                    if (response.Body is not null)
                    {
                        results.Add(response.Body);
                    }
                }


                // assert
                results.Select(x => x.RootElement.ToString()).ToList().MatchSnapshot();
            });
        }

        [Fact]
        public async Task Execution_Error()
        {
            Snapshot.FullName();

            await TryTest(async ct =>
            {
                // arrange
                using IWebHost host = TestServerHelper.CreateServer(
                    x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                    out var port);

                var serviceCollection = new ServiceCollection();
                serviceCollection
                    .AddProtocol<GraphQLWebSocketProtocolFactory>()
                    .AddWebSocketClient(
                        "Foo",
                        c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

                IServiceProvider services = serviceCollection.BuildServiceProvider();
                ISessionPool sessionPool = services.GetRequiredService<ISessionPool>();

                List<JsonDocument> results = new();
                MockDocument document = new("subscription Test { onTest }");
                OperationRequest request = new("Test", document);

                // act
                var connection =
                    new WebSocketConnection(async _ => await sessionPool.CreateAsync("Foo", _));
                await foreach (var response in connection.ExecuteAsync(request, ct))
                {
                    if (response.Body is not null)
                    {
                        results.Add(response.Body);
                    }
                }

                // assert
                results.Select(x => x.RootElement.ToString()).ToList().MatchSnapshot();
            });
        }

        [Fact]
        public async Task Validation_Error()
        {
            Snapshot.FullName();

            await TryTest(async ct =>
            {
                // arrange
                using IWebHost host = TestServerHelper.CreateServer(
                    x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                    out var port);
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
                var connection =
                    new WebSocketConnection(async _ => await sessionPool.CreateAsync("Foo", _));
                await foreach (var response in connection.ExecuteAsync(request, ct))
                {
                    if (response.Body is not null)
                    {
                        results.Add(response.Body);
                    }
                }

                // assert
                results.Select(x => x.RootElement.ToString()).ToList().MatchSnapshot();
            });
        }

        [Fact]
        public async Task Request_With_ConnectionPayload()
        {
            Snapshot.FullName();

            await TryTest(async ct =>
            {
                // arrange
                var payload = new Dictionary<string, object> { ["Key"] = "Value" };
                var sessionInterceptor = new StubSessionInterceptor();
                using IWebHost host = TestServerHelper.CreateServer(
                    x => x
                        .AddTypeExtension<StringSubscriptionExtensions>()
                        .AddSocketSessionInterceptor<ISocketSessionInterceptor>(
                            _ => sessionInterceptor),
                    out var port);

                var serviceCollection = new ServiceCollection();
                serviceCollection
                    .AddProtocol<GraphQLWebSocketProtocolFactory>()
                    .AddWebSocketClient(
                        "Foo",
                        c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"))
                    .ConfigureConnectionInterceptor(new StubConnectionInterceptor(payload));
                IServiceProvider services =
                    serviceCollection.BuildServiceProvider();

                ISessionPool sessionPool =
                    services.GetRequiredService<ISessionPool>();

                List<JsonDocument> results = new();
                MockDocument document = new("subscription Test { onTest(id:1) }");
                OperationRequest request = new("Test", document);

                // act
                var connection =
                    new WebSocketConnection(async _ => await sessionPool.CreateAsync("Foo", _));
                await foreach (var response in connection.ExecuteAsync(request, ct))
                {
                    if (response.Body is not null)
                    {
                        results.Add(response.Body);
                    }
                }

                // assert
                Dictionary<string, object> message =
                    Assert.IsType<Dictionary<string, object>>(
                        sessionInterceptor.InitializeConnectionMessage?.Payload);

                Assert.Equal(payload["Key"], message["Key"]);
            });
        }

        [Fact]
        public async Task Parallel_Request_SameSocket()
        {
            Snapshot.FullName();

            await TryTest(async ct =>
            {
                // arrange
                using IWebHost host = TestServerHelper
                    .CreateServer(
                        x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                        out var port);

                ServiceCollection serviceCollection = new();
                serviceCollection
                    .AddProtocol<GraphQLWebSocketProtocolFactory>()
                    .AddWebSocketClient(
                        "Foo",
                        c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
                IServiceProvider services = serviceCollection.BuildServiceProvider();

                ISessionPool sessionPool = services.GetRequiredService<ISessionPool>();
                ConcurrentDictionary<int, List<JsonDocument>> results = new();

                async Task CreateSubscription(int id)
                {
                    var connection = new WebSocketConnection(
                        async cancellationToken =>
                            await sessionPool.CreateAsync("Foo", cancellationToken));
                    var document = new MockDocument(
                        $"subscription Test {{ onTest(id:{id.ToString()}) }}");
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
                foreach (KeyValuePair<int, List<JsonDocument>> sub in results.OrderBy(x => x.Key))
                {
                    JsonDocument[] jsonDocuments = sub.Value.ToArray();

                    str += "Operation " + sub.Key + "\n";
                    for (var index = 0; index < jsonDocuments.Length; index++)
                    {
                        str += "Operation " + jsonDocuments[index].RootElement + "\n";
                    }
                }

                str.MatchSnapshot();
            });
        }

        [ExtendObjectType("Subscription")]
        public class StringSubscriptionExtensions
        {
            [SubscribeAndResolve]
            public async IAsyncEnumerable<string> OnTest(int? id)
            {
                if (id is null)
                {
                    throw new Exception();
                }

                for (var i = 0; i < 10; i++)
                {
                    await Task.Delay(1);
                    yield return $"{id ?? 0}num{i}";
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

            public DocumentHash Hash { get; } = new("MD5", "ABC");
        }

        private class StubSessionInterceptor : DefaultSocketSessionInterceptor
        {
            public override ValueTask<ConnectionStatus> OnConnectAsync(
                ISocketConnection connection,
                InitializeConnectionMessage message,
                CancellationToken cancellationToken)
            {
                InitializeConnectionMessage = message;
                return new ValueTask<ConnectionStatus>(ConnectionStatus.Accept());
            }

            public InitializeConnectionMessage? InitializeConnectionMessage { get; private set; }
        }

        private class StubConnectionInterceptor : ISocketConnectionInterceptor
        {
            private readonly object? _payload;

            public StubConnectionInterceptor(object? payload)
            {
                _payload = payload;
            }

            public ValueTask<object?> CreateConnectionInitPayload(
                ISocketProtocol protocol,
                CancellationToken cancellationToken)
            {
                return new(_payload);
            }
        }
    }
}
