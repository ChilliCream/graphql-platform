using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets.Protocols;
using static HotChocolate.Tests.TestHelper;

namespace StrawberryShake.Transport.WebSockets;

public class IntegrationTests : ServerTestBase
{
    public IntegrationTests(TestServerFactory serverFactory)
        : base(serverFactory) { }

    [Fact]
    public async Task Simple_Request()
    {
        var snapshot = Snapshot.Create();

        await TryTest(
            async ct =>
            {
                // arrange
                using var host = TestServerHelper.CreateServer(
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

                var sessionPool =
                    services.GetRequiredService<ISessionPool>();

                List<JsonDocument> results = [];
                MockDocument document = new("subscription Test { onTest(id:1) }");
                OperationRequest request = new("Test", document);

                // act
                var connection =
                    new WebSocketConnection(async _ => await sessionPool.CreateAsync("Foo", _));

                await foreach (var response in
                    connection.ExecuteAsync(request).WithCancellation(ct))
                {
                    if (response.Body is not null)
                    {
                        results.Add(response.Body);
                    }
                }


                // assert
                await snapshot
                    .Add(results.Select(x => x.RootElement.ToString()).ToList())
                    .MatchAsync(ct);
            });
    }

    [Fact]
    public async Task Execution_Error()
    {
        var snapshot = Snapshot.Create();

        await TryTest(
            async ct =>
            {
                // arrange
                using var host = TestServerHelper.CreateServer(
                    x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                    out var port);

                var serviceCollection = new ServiceCollection();
                serviceCollection
                    .AddProtocol<GraphQLWebSocketProtocolFactory>()
                    .AddWebSocketClient(
                        "Foo",
                        c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

                IServiceProvider services = serviceCollection.BuildServiceProvider();
                var sessionPool = services.GetRequiredService<ISessionPool>();

                List<JsonDocument> results = [];
                MockDocument document = new("subscription Test { onTest }");
                OperationRequest request = new("Test", document);

                // act
                var connection =
                    new WebSocketConnection(async _ => await sessionPool.CreateAsync("Foo", _));

                await foreach (var response in
                    connection.ExecuteAsync(request).WithCancellation(ct))
                {
                    if (response.Body is not null)
                    {
                        results.Add(response.Body);
                    }
                }

                // assert
                await snapshot
                    .Add(results.Select(x => x.RootElement.ToString()).ToList())
                    .MatchAsync(ct);
            });
    }

    [Fact]
    public async Task Validation_Error()
    {
        var snapshot = Snapshot.Create();

        await TryTest(
            async ct =>
            {
                // arrange
                using var host = TestServerHelper.CreateServer(
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

                var sessionPool =
                    services.GetRequiredService<ISessionPool>();

                List<string> results = [];
                MockDocument document = new(@"subscription Test { onTest(id:""Foo"") }");
                OperationRequest request = new("Test", document);

                // act
                var connection =
                    new WebSocketConnection(async _ => await sessionPool.CreateAsync("Foo", _));

                await foreach (var response in
                    connection.ExecuteAsync(request).WithCancellation(ct))
                {
                    if (response.Exception is not null)
                    {
                        results.Add(response.Exception.Message);
                    }
                }

                // assert
                await snapshot.Add(results).MatchAsync(ct);
            });
    }

    [Fact]
    public async Task Request_With_ConnectionPayload()
    {
        var snapshot = Snapshot.Create();

        await TryTest(
            async ct =>
            {
                // arrange
                var payload = new Dictionary<string, object> { ["Key"] = "Value", };
                var sessionInterceptor = new StubSessionInterceptor();
                using var host = TestServerHelper.CreateServer(
                    builder => builder
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

                IServiceProvider services = serviceCollection.BuildServiceProvider();
                var sessionPool = services.GetRequiredService<ISessionPool>();

                List<string> results = [];
                MockDocument document = new("subscription Test { onTest(id:1) }");
                OperationRequest request = new("Test", document);

                // act
                var connection =
                    new WebSocketConnection(async _ => await sessionPool.CreateAsync("Foo", _));

                await foreach (var response in
                    connection.ExecuteAsync(request).WithCancellation(ct))
                {
                    if (response.Body is not null)
                    {
                        results.Add(response.Body.RootElement.ToString());
                    }
                }

                // assert
                var message =
                    Assert.IsType<Dictionary<string, string>>(
                        sessionInterceptor.InitializeConnectionMessage);
                Assert.Equal(payload["Key"], message["Key"]);
                await snapshot.Add(results).MatchAsync(ct);
            });
    }

    [Fact]
    public async Task Parallel_Request_SameSocket()
    {
        var snapshot = Snapshot.Create();

        await TryTest(
            async ct =>
            {
                // arrange
                using var host = TestServerHelper
                    .CreateServer(
                        x => x.AddTypeExtension<StringSubscriptionExtensions>(),
                        out var port);

                ServiceCollection serviceCollection = [];
                serviceCollection
                    .AddProtocol<GraphQLWebSocketProtocolFactory>()
                    .AddWebSocketClient(
                        "Foo",
                        c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
                IServiceProvider services = serviceCollection.BuildServiceProvider();

                var sessionPool = services.GetRequiredService<ISessionPool>();
                ConcurrentDictionary<int, List<JsonDocument>> results = new();

                async Task CreateSubscription(int id)
                {
                    var connection = new WebSocketConnection(
                        async cancellationToken =>
                            await sessionPool.CreateAsync("Foo", cancellationToken));
                    var document = new MockDocument(
                        $"subscription Test {{ onTest(id:{id.ToString()}) }}");
                    var request = new OperationRequest("Test", document);

                    await foreach (var response in
                        connection.ExecuteAsync(request).WithCancellation(ct))
                    {
                        if (response.Body is not null)
                        {
                            results.AddOrUpdate(
                                id,
                                _ => [response.Body,],
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
                    var jsonDocuments = sub.Value.ToArray();

                    str += "Operation " + sub.Key + "\n";

                    for (var index = 0; index < jsonDocuments.Length; index++)
                    {
                        str += "Operation " + jsonDocuments[index].RootElement + "\n";
                    }
                }

                await snapshot.Add(str).MatchAsync(ct);
            });
    }

    [ExtendObjectType("Subscription")]
    public class StringSubscriptionExtensions
    {
        public async IAsyncEnumerable<string> OnTestSubscribe(int? id)
        {
            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(1);
                yield return $"{id ?? 0}num{i}";
            }
        }

        [Subscribe(With = nameof(OnTestSubscribe))]
        public string OnTest(int? id, [EventMessage] string payload)
        {
            if (id is null)
            {
                throw new Exception();
            }

            return payload;
        }


#pragma warning disable CS0618
        [SubscribeAndResolve]
#pragma warning restore CS0618
        public async IAsyncEnumerable<int> CountUp()
        {
            for (var i = 0; i < 100; i++)
            {
                await Task.Delay(1);
                yield return i;
            }
        }
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

    private sealed class StubSessionInterceptor : DefaultSocketSessionInterceptor
    {
        public override ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketSession session,
            IOperationMessagePayload connectionInitMessage,
            CancellationToken cancellationToken = default)
        {
            InitializeConnectionMessage = connectionInitMessage.As<Dictionary<string, string>>();
            return base.OnConnectAsync(session, connectionInitMessage, cancellationToken);
        }

        public Dictionary<string, string>? InitializeConnectionMessage { get; private set; }
    }

    private sealed class StubConnectionInterceptor : ISocketConnectionInterceptor
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
