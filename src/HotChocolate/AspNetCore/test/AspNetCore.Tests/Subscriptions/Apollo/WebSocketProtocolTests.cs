using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.Apollo;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Transport.Formatters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Subscriptions.Apollo;

public class WebSocketProtocolTests(TestServerFactory serverFactory)
    : SubscriptionTestBase(serverFactory)
{
    [Fact]
    public Task Send_Connect_AcceptAndKeepAlive()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message.RootElement.GetProperty("type").GetString());
        });

    [Fact]
    public Task Send_Connect_To_Many_Init_Messages()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            await webSocket.SendConnectionInitializeAsync(ct);
            await WaitForMessage(webSocket, "connection_ack", ct);

            // act
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue);
            Assert.Equal(WebSocketCloseStatus.ProtocolError, webSocket.CloseStatus.Value);
        });

    [Fact]
    public Task No_ConnectionInit_Timeout()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQL()
                    .ModifyServerOptions(o =>
                    {
                        o.Sockets.ConnectionInitializationTimeout = TimeSpan.FromMilliseconds(50);
                        o.Sockets.KeepAliveInterval = TimeSpan.FromMilliseconds(150);
                    }));
            var client = CreateWebSocketClient(testServer);

            // act
            using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(WebSocketCloseStatus.ProtocolError, webSocket.CloseStatus.Value);
        });

    [Fact]
    public Task Send_Connect_With_Auth_Accept()
        => TryTest(async ct =>
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
            await webSocket.SendConnectionInitializeAsync(new() { ["token"] = "abc " }, ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message.RootElement.GetProperty(MessageProperties.Type).GetString());
        });

    [Fact]
    public Task Send_Connect_With_Auth_Reject()
        => TryTest(async ct =>
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
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            await WaitForMessage(webSocket, "connection_error", ct);
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(WebSocketCloseStatus.NormalClosure, webSocket.CloseStatus.Value);
        });

    [Fact]
    public Task Send_Connect_AcceptAndKeepAlive_Explicit_Route()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateServer(b => b.MapGraphQLWebSocket());
            var client = CreateWebSocketClient(testServer);
            var webSocket = await client.ConnectAsync(
                new("ws://localhost:5000/graphql/ws"),
                ct);

            // act
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message.RootElement.GetProperty("type").GetString());
        });

    [Fact]
    public Task Send_Connect_AcceptAndKeepAlive_Explicit_Route_Explicit_Path()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateServer(b => b.MapGraphQLWebSocket("/foo/bar"));
            var client = CreateWebSocketClient(testServer);
            var webSocket = await client.ConnectAsync(
                new("ws://localhost:5000/foo/bar"),
                ct);

            // act
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            var message = await webSocket.ReceiveServerMessageAsync(ct);
            Assert.NotNull(message);
            Assert.Equal("connection_ack", message.RootElement.GetProperty("type").GetString());
        });

    [Fact]
    public Task Send_Terminate()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            var webSocket = await ConnectToServerAsync(client, ct);

            // act
            await webSocket.SendTerminateConnectionAsync(ct);

            // assert
            var buffer = new byte[1024];
            await webSocket.ReceiveAsync(buffer, ct);
            Assert.True(webSocket.CloseStatus.HasValue);
            Assert.Equal(WebSocketCloseStatus.NormalClosure, webSocket.CloseStatus.Value);
        });

    [Fact]
    public Task Connect_With_Invalid_Protocol()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = testServer.CreateWebSocketClient();

            client.ConfigureRequest = r => r.Headers.SecWebSocketProtocol = "foo";

            // act
            var socket = await client.ConnectAsync(SubscriptionUri, ct);
            var buffer = new byte[1024];
            await socket.ReceiveAsync(buffer, ct);

            // assert
            Assert.True(socket.CloseStatus.HasValue);
            Assert.Equal(WebSocketCloseStatus.ProtocolError, socket.CloseStatus.Value);
        });

    // TODO : FIX Flaky Test
    [Fact(Skip = "Flaky")]
    public Task Send_Start_ReceiveDataOnMutation()
    {
        var snapshot = new Snapshot();

        return TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            var webSocket = await ConnectToServerAsync(client, ct);

            var document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");
            var request = new GraphQLRequest(document);
            const string subscriptionId = "abc";

            // act
            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // assert
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
                        """
                });

            var message = await WaitForMessage(webSocket, "data", ct);
            Assert.NotNull(message);
            await snapshot.Add(message).MatchAsync(ct);
        });
    }

    [Fact]
    public Task Send_Start_Id_Not_Unique()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            var webSocket = await ConnectToServerAsync(client, ct);

            var document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");
            var request = new GraphQLRequest(document);
            const string subscriptionId = "abc";

            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // act
            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue);
            Assert.Equal(WebSocketCloseStatus.InternalServerError, webSocket.CloseStatus.Value);
        });

    [Fact]
    public Task Send_Subscribe_No_Id()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            using var webSocket = await ConnectToServerAsync(client, ct);

            // act

            await webSocket.SendMessageAsync("{ \"type\": \"subscribe\" }", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue);
            Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus.Value);
        });

    [Fact]
    public Task Send_Subscribe_Empty_Id()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            using var webSocket = await ConnectToServerAsync(client, ct);

            // act

            await webSocket.SendMessageAsync("{ \"type\": \"subscribe\", \"id\": \"\" }", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue);
            Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus.Value);
        });

    [Fact]
    public Task Send_Subscribe_SyntaxError()
    {
        var snapshot = new Snapshot();

        return TryTest(
            async ct =>
            {
                // arrange
                snapshot.Clear();
                using var testServer = CreateStarWarsServer();
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act

                await webSocket.SendMessageAsync(
                    // lang=json
                    """
                    {
                        "type": "start",
                        "id": "abc",
                        "payload": {
                            "query": "}"
                        }
                    }
                    """,
                    ct);

                // assert
                var message = await WaitForMessage(webSocket, "error", ct);
                await snapshot.Add(message).MatchAsync(ct);
            });
    }

    [Fact]
    public Task Send_Subscribe_SyntaxError_No_Id()
        => TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer();
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act

                await webSocket.SendMessageAsync(
                    // lang=json
                    """
                    {
                        "type": "start",
                        "id": "",
                        "payload": {
                            "query": "}"
                        }
                    }
                    """,
                    ct);

                // assert
                await webSocket.ReceiveServerMessageAsync(ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus.Value);
            });

    [Fact]
    public Task Send_Start_Stop()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            var webSocket = await ConnectToServerAsync(client, ct);

            var document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");

            var request = new GraphQLRequest(document);

            const string subscriptionId = "abc";

            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            await testServer.SendPostRequestAsync(new ClientQueryRequest
            {
                Query =
                    """
                    mutation {
                        createReview(
                            episode: NEW_HOPE
                            review: {
                                commentary: "foo"
                                stars: 5
                            }
                        ) {
                            stars
                        }
                    }
                    """
            });

            await WaitForMessage(webSocket, "data", ct);

            // act
            await webSocket.SendSubscriptionStopAsync(subscriptionId, ct);

            await testServer.SendPostRequestAsync(new ClientQueryRequest
            {
                Query =
                    """
                    mutation {
                        createReview(
                            episode: NEW_HOPE
                            review: {
                                commentary: "foo"
                                stars: 5
                            }
                        ) {
                            stars
                        }
                    }
                    """
            });

            // assert
            var message = await WaitForMessage(webSocket, "data", TimeSpan.FromSeconds(5), ct);
            Assert.Null(message);
        });

    [Fact]
    public Task Send_Start_ValidationError()
    {
        var snapshot = new Snapshot();

        return TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            var webSocket = await ConnectToServerAsync(client, ct);

            var document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { _stars } }");
            var request = new GraphQLRequest(document);
            const string subscriptionId = "abc";

            // act
            await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

            // assert
            var message = await WaitForMessage(webSocket, "error", ct);
            Assert.NotNull(message);
            await snapshot.Add(message).MatchAsync(ct);
        });
    }

    [Fact]
    public Task Send_Invalid_Message_String()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendMessageAsync("hello", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(WebSocketCloseStatus.InternalServerError, webSocket.CloseStatus.Value);
        });

    [Fact]
    public Task Send_Invalid_Message_No_Type()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendMessageAsync("{ }", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus.Value);
        });

    [Fact]
    public Task Send_Invalid_Message_Invalid_Type()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            using var webSocket = await ConnectToServerAsync(client, ct);

            // act
            await webSocket.SendMessageAsync("{ \"type\": \"abc\" }", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus.Value);
        });

    [Fact]
    public Task Send_Invalid_Message_Not_An_Object()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendMessageAsync("[]", ct);

            // assert
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(CloseReasons.InvalidMessage, (int)webSocket.CloseStatus.Value);
        });

    // TODO : FIX Flaky Test
    [Fact(Skip = "Flaky")]
    public Task Send_Start_ReceiveDataOnMutation_StripNull() =>
        TryTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(
                    configureServices: c =>
                        c.AddGraphQL()
                            .AddWebSocketPayloadFormatter(
                                _ => new DefaultWebSocketPayloadFormatter(
                                    new WebSocketPayloadFormatterOptions
                                    {
                                        Json = new JsonResultFormatterOptions
                                        {
                                            NullIgnoreCondition = JsonNullIgnoreCondition.FieldsAndLists
                                        }
                                    })));
                var client = CreateWebSocketClient(testServer);
                var webSocket = await ConnectToServerAsync(client, ct);

                var document = Utf8GraphQLParser.Parse(
                    "subscription { onReview(episode: NEW_HOPE) { stars, commentary } }");
                var request = new GraphQLRequest(document);
                const string subscriptionId = "abc";

                // act
                await webSocket.SendSubscriptionStartAsync(subscriptionId, request);

                // assert
                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query =
                            """
                            mutation {
                                createReview(episode: NEW_HOPE review: {
                                    stars: 5
                                    commentary: null
                                }) {
                                    stars
                                    commentary
                                }
                            }
                            """
                    });

                var message = await WaitForMessage(webSocket, "data", ct);
                Assert.NotNull(message);
                var messagePayload = message.RootElement.GetProperty("payload");
                var messageData = messagePayload.GetProperty("data");
                var messageOnReview = messageData.GetProperty("onReview");
                Assert.False(messageOnReview.TryGetProperty("commentary", out _));
            });

    [Fact]
    public Task Connection_Reject_With_Long_Message_Should_Close_Cleanly()
        => TryTest(async ct =>
        {
            // arrange
            // a rejection message longer than the 123 UTF-8 byte close-frame limit.
            var longMessage = new string('a', 140);
            var interceptor = new RejectInterceptor(longMessage);
            using var testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor));
            var client = CreateWebSocketClient(testServer);
            using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

            // act
            await webSocket.SendConnectionInitializeAsync(ct);

            // assert
            var error = await WaitForMessage(webSocket, "connection_error", ct);
            Assert.NotNull(error);
            Assert.Equal(
                longMessage,
                error.RootElement.GetProperty("payload").GetProperty("message").GetString());
            await webSocket.ReceiveServerMessageAsync(ct);
            Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
            Assert.Equal(WebSocketCloseStatus.NormalClosure, webSocket.CloseStatus.Value);
        });

    [Fact]
    public Task Connection_Terminate_Should_Invoke_OnClose_Once()
        => TryTest(async ct =>
        {
            // arrange
            var interceptor = new OnCloseCountingInterceptor();
            using var testServer = CreateStarWarsServer(
                configureServices: s => s
                    .AddGraphQLServer()
                    .AddSocketSessionInterceptor(_ => interceptor));
            var client = CreateWebSocketClient(testServer);
            var webSocket = await ConnectToServerAsync(client, ct);

            // act
            await webSocket.SendTerminateConnectionAsync(ct);

            // assert
            var buffer = new byte[1024];
            await webSocket.ReceiveAsync(buffer, ct);
            await WaitForConditions(() => interceptor.OnCloseCount >= 1, ct);
            await Task.Delay(1000, ct);
            Assert.Equal(1, interceptor.OnCloseCount);
        });

    [Fact]
    public Task Client_KeepAlive_Should_Not_Close_Connection()
        => TryTest(async ct =>
        {
            // arrange
            using var testServer = CreateStarWarsServer();
            var client = CreateWebSocketClient(testServer);
            var webSocket = await ConnectToServerAsync(client, ct);

            // act
            await webSocket.SendMessageAsync("{\"type\":\"ka\"}", ct);

            // assert
            // the connection must stay open and keep processing subsequent messages.
            var document = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { _stars } }");
            var request = new GraphQLRequest(document);
            await webSocket.SendSubscriptionStartAsync("abc", request);
            var message = await WaitForMessage(webSocket, "error", ct);
            Assert.NotNull(message);
            Assert.Equal("error", message.RootElement.GetProperty("type").GetString());
        });

    private sealed class RejectInterceptor(string message) : DefaultSocketSessionInterceptor
    {
        public override ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketSession session,
            IOperationMessagePayload connectionInitMessage,
            CancellationToken cancellationToken = default)
            => new(ConnectionStatus.Reject(message));
    }

    private sealed class OnCloseCountingInterceptor : DefaultSocketSessionInterceptor
    {
        private int _onCloseCount;

        public int OnCloseCount => _onCloseCount;

        public override ValueTask OnCloseAsync(
            ISocketSession session,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _onCloseCount);
            return default;
        }
    }

    private class AuthInterceptor : DefaultSocketSessionInterceptor
    {
        public override ValueTask<ConnectionStatus> OnConnectAsync(
            ISocketSession session,
            IOperationMessagePayload connectionInitMessage,
            CancellationToken cancellationToken = default)
        {
            var payload = connectionInitMessage.Payload?.Deserialize<Auth>();

            if (payload?.Token is not null)
            {
                return base.OnConnectAsync(session, connectionInitMessage, cancellationToken);
            }

            return new(ConnectionStatus.Reject());
        }

        private sealed class Auth
        {
            [JsonPropertyName("token")]
            public string? Token { get; set; }
        }
    }
}
