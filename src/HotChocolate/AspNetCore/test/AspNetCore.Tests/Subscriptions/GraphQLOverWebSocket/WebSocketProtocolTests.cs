using System.Diagnostics;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.GraphQLOverWebSocket;
using HotChocolate.Subscriptions.Diagnostics;
using HotChocolate.Transport;
using HotChocolate.Transport.Sockets.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static System.Net.WebSockets.WebSocketCloseStatus;

namespace HotChocolate.AspNetCore.Subscriptions.GraphQLOverWebSocket;

public class WebSocketProtocolTests : SubscriptionTestBase
{
    private readonly ITestOutputHelper _output;

    public WebSocketProtocolTests(TestServerFactory serverFactory, ITestOutputHelper output)
        : base(serverFactory)
    {
        _output = output;
    }

    [Fact]
    public Task Send_Connect_Accept()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                await webSocket.SendConnectionInitAsync(ct);

                // assert
                var message = await webSocket.ReceiveServerMessageAsync(ct);
                Assert.NotNull(message);
                Assert.Equal(Messages.ConnectionAccept, message![MessageProperties.Type]);
            });

    [Fact]
    public Task Send_Multiple_Connect_Messages_Close_Connection()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                await webSocket.SendConnectionInitAsync(ct);
                await WaitForMessage(webSocket, Messages.ConnectionAccept, ct);

                // act
                await webSocket.SendConnectionInitAsync(ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.TooManyInitAttempts, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Connect_Accept_Ping()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(
                    configureConventions: mapping => mapping.WithOptions(
                        new GraphQLServerOptions
                        {
                            Sockets =
                            {
                                ConnectionInitializationTimeout =
                                    TimeSpan.FromMilliseconds(1000),
                                KeepAliveInterval = TimeSpan.FromMilliseconds(150),
                            },
                        }));
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                await webSocket.SendConnectionInitAsync(ct);

                // assert
                await WaitForMessage(webSocket, Messages.ConnectionAccept, ct);
                var message = await WaitForMessage(
                    webSocket,
                    Messages.Ping,
                    TimeSpan.FromSeconds(5),
                    ct);
                Assert.NotNull(message);
                Assert.Equal(Messages.Ping, message![MessageProperties.Type]);
            });

    [Fact]
    public Task No_ConnectionInit_Timeout()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(
                    configureConventions: mapping => mapping.WithOptions(
                        new GraphQLServerOptions
                        {
                            Sockets =
                            {
                                ConnectionInitializationTimeout = TimeSpan.FromMilliseconds(50),
                                KeepAliveInterval = TimeSpan.FromMilliseconds(150),
                            },
                        }));
                var client = CreateWebSocketClient(testServer);

                // act
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(
                    CloseReasons.ConnectionInitWaitTimeout,
                    (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Connect_With_Auth_Accept()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new AuthInterceptor();
                using var testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                await webSocket.SendConnectionInitAsync(new() { ["token"] = "abc ", }, ct);

                // assert
                var message = await webSocket.ReceiveServerMessageAsync(ct);
                Assert.NotNull(message);
                Assert.Equal(Messages.ConnectionAccept, message![MessageProperties.Type]);
            });

    [Fact]
    public Task Send_Connect_With_Auth_Reject()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new AuthInterceptor();
                using var testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                await webSocket.SendConnectionInitAsync(ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(CloseReasons.Unauthorized, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Connect_Accept_Explicit_Route()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateServer(b => b.MapGraphQLWebSocket());
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(
                    new("ws://localhost:5000/graphql/ws"),
                    ct);

                // act
                await webSocket.SendConnectionInitAsync(ct);

                // assert
                var message = await webSocket.ReceiveServerMessageAsync(ct);
                Assert.NotNull(message);
                Assert.Equal("connection_ack", message!["type"]);
            });

    [Fact]
    public Task Send_Connect_Accept_Explicit_Route_Explicit_Path()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateServer(b => b.MapGraphQLWebSocket("/foo/bar"));
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(
                    new("ws://localhost:5000/foo/bar"),
                    ct);

                // act
                await webSocket.SendConnectionInitAsync(ct);

                // assert
                var message = await webSocket.ReceiveServerMessageAsync(ct);
                Assert.NotNull(message);
                Assert.Equal("connection_ack", message!["type"]);
            });

    [Fact]
    public Task Connect_With_Invalid_Protocol()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = testServer.CreateWebSocketClient();

                // act
                client.ConfigureRequest = r => r.Headers.SecWebSocketProtocol = "foo";
                using var socket = await client.ConnectAsync(SubscriptionUri, ct);

                // assert
                await socket.ReceiveServerMessageAsync(ct);
                Assert.True(socket.CloseStatus.HasValue);
                Assert.Equal(ProtocolError, socket.CloseStatus!.Value);
            });

    [Fact]
    public Task Subscribe_ReceiveDataOnMutation()
    {
        var snapshot = new Snapshot();

        return TryTest(
            async ct =>
            {
                // arrange
                var diagnostics = new SubscriptionTestDiagnostics();
                using var testServer = CreateStarWarsServer(
                    configureServices: c =>
                    {
                        c.AddGraphQL()
                            .AddDiagnosticEventListener(_ => diagnostics);
                    },
                    output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                const string subscriptionId = "abc";

                // act
                await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

                while (diagnostics.Subscribed is not 1)
                {
                    await Task.Delay(10, ct);
                }

                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query =
                            """
                            mutation {
                                createReview(episode: NEW_HOPE review: {
                                    commentary: "foo"
                                    stars: 5
                                }) {
                                    stars
                                }
                            }
                            """,
                    });

                // assert
                var message = await WaitForMessage(webSocket, Messages.Next, ct);
                Assert.NotNull(message);
                await snapshot.Add(message).MatchAsync(ct);
            });
    }

    [Fact]
    public Task Subscribe_Id_Not_Unique()
    {
        return TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                const string subscriptionId = "abc";

                // act
                await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);
                await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.SubscriberNotUnique, (int)webSocket.CloseStatus!.Value);
            });
    }

    [Fact]
    public Task Send_Subscribe_No_Auth_Close()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                const string subscriptionId = "abc";
                await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.Unauthorized, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Subscribe_No_Id()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act

                await webSocket.SendMessageAsync("{ \"type\": \"subscribe\" }", ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.ProtocolError, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Subscribe_Empty_Id()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act

                await webSocket.SendMessageAsync("{ \"type\": \"subscribe\", \"id\": \"\" }", ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.ProtocolError, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public async Task Send_Subscribe_Complete()
    {
        await TryTest(
            async ct =>
            {
                // arrange
                var diagnostics = new SubscriptionTestDiagnostics();
                using var testServer = CreateStarWarsServer(
                    configureServices: services
                        => services
                            .AddGraphQL()
                            .AddDiagnosticEventListener(_ => diagnostics),
                    output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                const string subscriptionId = "abc";
                await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query = @"
                            mutation {
                                createReview(episode:NEW_HOPE review: {
                                    commentary: ""foo""
                                    stars: 5
                                }) {
                                    stars
                                }
                            }",
                    });

                await WaitForMessage(webSocket, Messages.Next, ct);

                // act
                await webSocket.SendCompleteAsync(subscriptionId, ct);

                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query = @"
                    mutation {
                        createReview(episode:NEW_HOPE review: {
                            commentary: ""foo""
                            stars: 5
                        }) {
                            stars
                        }
                    }",
                    });

                // assert
                var message = await WaitForMessage(webSocket, Messages.Next, ct);

                Assert.Null(message);
                Assert.True(diagnostics.UnsubscribeInvoked, "UnsubscribeInvoked is false");
                Assert.True(diagnostics.CloseInvoked, "CloseInvoked is false");
            });
    }

    [Fact]
    public async Task Send_Subscribe_Complete_From_Server()
    {
        await TryTest(
            async ct =>
            {
                // arrange
                var diagnostics = new SubscriptionTestDiagnostics();
                using var testServer = CreateStarWarsServer(
                    configureServices: services
                        => services
                            .AddGraphQL()
                            .AddDiagnosticEventListener(_ => diagnostics),
                    output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                const string subscriptionId = "abc";
                await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query = @"
                    mutation {
                        createReview(episode:NEW_HOPE review: {
                            commentary: ""foo""
                            stars: 5
                        }) {
                            stars
                        }
                    }",
                    });

                await WaitForMessage(webSocket, Messages.Next, ct);

                // act
                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query = @"
                    mutation {
                        complete(episode:NEW_HOPE)
                    }",
                    });

                // assert
                await WaitForMessage(webSocket, Messages.Complete, ct);

                Assert.True(diagnostics.UnsubscribeInvoked, "UnsubscribeInvoked is false");
                Assert.True(diagnostics.CloseInvoked, "CloseInvoked is false");
            });
    }

    [Fact]
    public async Task Send_Subscribe_100x_Complete_From_Server()
    {
        await TryTest(
            async ct =>
            {
                // arrange
                var diagnostics = new SubscriptionTestDiagnostics();
                using var testServer = CreateStarWarsServer(
                    configureServices: services
                        => services
                            .AddGraphQL()
                            .AddDiagnosticEventListener(_ => diagnostics),
                    output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");

                var stopwatch = Stopwatch.StartNew();

                for (var i = 0; i < 100; i++)
                {
                    await webSocket.SendSubscribeAsync(i.ToString(), payload, ct);
                }

                while(diagnostics.Subscribed < 100)
                {
                    await Task.Delay(10, ct);
                }

                _output.WriteLine($"Subscribed in {stopwatch.ElapsedMilliseconds}ms");

                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query = @"
                            mutation {
                                createReview(episode:NEW_HOPE review: {
                                    commentary: ""foo""
                                    stars: 5
                                }) {
                                    stars
                                }
                            }",
                    });

                for (var i = 0; i < 100; i++)
                {
                    await WaitForMessage(webSocket, Messages.Next, ct);
                }

                // act
                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query = @"mutation { complete(episode:NEW_HOPE) }",
                    });

                // assert
                for (var i = 0; i < 100; i++)
                {
                    await WaitForMessage(webSocket, Messages.Complete, ct);
                }

                Assert.True(diagnostics.UnsubscribeInvoked, "UnsubscribeInvoked is false");
                Assert.True(diagnostics.CloseInvoked, "CloseInvoked is false");
            });
    }

    [Fact]
    public Task Send_Subscribe_SyntaxError()
    {
        var snapshot = new Snapshot();

        return TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { 123 } }");
                const string subscriptionId = "abc";

                // act
                await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

                // assert
                var message = await WaitForMessage(webSocket, Messages.Error, ct);
                Assert.NotNull(message);
                await snapshot.Add(message).MatchAsync(ct);
            });
    }

    [Fact]
    public Task Send_Subscribe_ValidationError()
    {
        var snapshot = new Snapshot();

        return TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { _stars } }");
                const string subscriptionId = "abc";

                // act
                await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

                // assert
                var message = await WaitForMessage(webSocket, Messages.Error, ct);
                Assert.NotNull(message);
                await snapshot.Add(message).MatchAsync(ct);
            });
    }

    [Fact]
    public Task Send_Ping()
    {
        var snapshot = new Snapshot();

        return TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new PingPongInterceptor();
                using var testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act
                await webSocket.SendPingAsync(ct);

                // assert
                var message = await WaitForMessage(webSocket, Messages.Pong, ct);
                Assert.NotNull(message);
                await snapshot.Add(message).MatchAsync(ct);
            });
    }

    [Fact]
    public Task Send_Ping_With_Payload()
    {
        var snapshot = new Snapshot();

        return TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new PingPongInterceptor();
                using var testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act
                await webSocket.SendPingAsync(
                    new Dictionary<string, object?> { ["abc"] = "def", },
                    ct);

                // assert
                var message = await WaitForMessage(webSocket, Messages.Pong, ct);
                Assert.NotNull(message);
                await snapshot.Add(message).MatchAsync(ct);
            });
    }

    [Fact]
    public Task Send_Pong()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new PingPongInterceptor();
                using var testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act
                await webSocket.SendPongAsync(ct);

                // assert
                await WaitForConditions(() => interceptor.OnPongInvoked, ct);
                Assert.Null(interceptor.Payload);
            });

    [Fact]
    public Task Send_Pong_With_Payload()
    {
        var snapshot = new Snapshot();

        return TryTest(
            async ct =>
            {
                // arrange
                snapshot.Clear();
                var interceptor = new PingPongInterceptor();
                using var testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act
                await webSocket.SendPongAsync(
                    new Dictionary<string, object?> { ["abc"] = "def", },
                    ct);

                // assert
                await WaitForConditions(() => interceptor.OnPongInvoked, ct);
                await snapshot.Add(interceptor.Payload).MatchAsync(ct);
            });
    }

    [Fact]
    public Task Send_Invalid_Message_String()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                await webSocket.SendMessageAsync("hello", ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(InternalServerError, webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Invalid_Message_No_Type()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                await webSocket.SendMessageAsync("{ }", ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(CloseReasons.ProtocolError, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Invalid_Message_Invalid_Type()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act
                await webSocket.SendMessageAsync("{ \"type\": \"abc\" }", ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(CloseReasons.ProtocolError, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Invalid_Message_Not_An_Object()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: _output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                await webSocket.SendMessageAsync("[]", ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(CloseReasons.ProtocolError, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Normal_Closure()
        => TryTest(
            async ct =>
            {
                // arrange
                var interceptor = new AuthInterceptor();
                using var testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQLServer()
                        .AddSocketSessionInterceptor(_ => interceptor));
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);
                await webSocket.SendConnectionInitAsync(ct);

                // act
                async Task Close() => await webSocket.CloseAsync(
                    NormalClosure,
                    "I want to close.",
                    ct);

                // assert
                var error = await Assert.ThrowsAsync<IOException>(Close);
                Assert.Equal("The remote end closed the connection.", error.Message);
            });

    [Fact]
    public Task Subscribe_Cancel()
    {
        return TryTest(
            async ct =>
            {
                // arrange
                var subscriptionRequest = new OperationRequest(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");

                using var testServer = CreateStarWarsServer(output: _output);
                var webSocketClient = CreateWebSocketClient(testServer);
                using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

                var client = await SocketClient.ConnectAsync(webSocket, ct);

                // act
                // ... subscribe
                var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

                try
                {
                    // ... we wait a second so we are fully subscribed ...
                    await socketResult.ReadResultsAsync()
                        .GetAsyncEnumerator(ct)
                        .MoveNextAsync()
                        .AsTask()
                        .WaitAsync(TimeSpan.FromSeconds(1), ct);
                }
                catch (TimeoutException)
                {
                    // ... we deliberately timeout here so we get back control ...
                }

                // ... next we complete the subscription from the client-side ... essentially cancel it.
                socketResult.Dispose();

                // ... now we wait another second to let the server unsubscribe cleanly ...
                await Task.Delay(500, ct);

                // ... we resubscribe to ensure the connection is still alive with no errors ...
                socketResult = await client.ExecuteAsync(subscriptionRequest, ct);

                try
                {
                    // ... we wait again ...
                    await socketResult.ReadResultsAsync()
                        .GetAsyncEnumerator(ct)
                        .MoveNextAsync()
                        .AsTask()
                        .WaitAsync(TimeSpan.FromSeconds(1), ct);
                }
                catch (TimeoutException)
                {
                    // ... we deliberately timeout here so we get back control ...
                }

                // .. and dispose it once more.
                socketResult.Dispose();
            });
    }

    private class AuthInterceptor : DefaultSocketSessionInterceptor
    {
        public override ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketSession session,
            IOperationMessagePayload connectionInitMessage,
            CancellationToken cancellationToken = default)
        {
            var payload = connectionInitMessage.As<Auth>();

            if (payload?.Token is not null)
            {
                return base.OnConnectAsync(session, connectionInitMessage, cancellationToken);
            }

            return new(ConnectionStatus.Reject());
        }

        private sealed class Auth
        {
            public string? Token { get; set; }
        }
    }

    private class PingPongInterceptor : DefaultSocketSessionInterceptor
    {
        public bool OnPongInvoked { get; private set; }

        public Dictionary<string, string?>? Payload { get; private set; }

        public override ValueTask<IReadOnlyDictionary<string, object?>?> OnPingAsync(
            ISocketSession session,
            IOperationMessagePayload pingMessage,
            CancellationToken cancellationToken = default)
        {
            var payload = pingMessage.As<Dictionary<string, string?>>();
            var responsePayload = new Dictionary<string, object?> { ["touched"] = true, };

            if (payload is not null)
            {
                foreach (var (key, value) in payload)
                {
                    responsePayload[key] = value;
                }
            }

            return new(responsePayload);
        }

        public override ValueTask OnPongAsync(
            ISocketSession session,
            IOperationMessagePayload pongMessage,
            CancellationToken cancellationToken = default)
        {
            OnPongInvoked = true;
            Payload = pongMessage.As<Dictionary<string, string?>>();
            return base.OnPongAsync(session, pongMessage, cancellationToken);
        }
    }

    public sealed class SubscriptionTestDiagnostics : SubscriptionDiagnosticEventsListener
    {
        private int _subscribed;

        public int Subscribed => _subscribed;

        public bool UnsubscribeInvoked { get; private set; }

        public bool CloseInvoked { get; private set; }

        public override void SubscribeSuccess(string topicName)
        {
            Interlocked.Increment(ref _subscribed);
        }

        public override void Unsubscribe(string topicName, int shard, int subscribers)
            => UnsubscribeInvoked = true;

        public override void Close(string topicName)
            => CloseInvoked = true;
    }
}
