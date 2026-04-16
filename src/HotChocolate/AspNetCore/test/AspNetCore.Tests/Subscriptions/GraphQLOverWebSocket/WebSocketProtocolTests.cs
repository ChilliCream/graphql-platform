using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.GraphQLOverWebSocket;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
using HotChocolate.Subscriptions.Diagnostics;
using HotChocolate.Text.Json;
using HotChocolate.Transport.Formatters;
using HotChocolate.Transport.Sockets.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static System.Net.WebSockets.WebSocketCloseStatus;
using OperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.AspNetCore.Subscriptions.GraphQLOverWebSocket;

public class WebSocketProtocolTests(TestServerFactory serverFactory, ITestOutputHelper output)
    : SubscriptionTestBase(serverFactory)
{
    [Fact]
    public Task Send_Connect_Accept()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                await webSocket.SendConnectionInitAsync(ct);

                // assert
                var message = await webSocket.ReceiveServerMessageAsync(ct);
                Assert.NotNull(message);
                Assert.Equal(Messages.ConnectionAccept, message.RootElement.GetProperty(MessageProperties.Type).GetString());
            });

    [Fact]
    public Task Send_Multiple_Connect_Messages_Close_Connection()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                await webSocket.SendConnectionInitAsync(ct);
                await WaitForMessage(webSocket, Messages.ConnectionAccept, ct);

                // act
                await webSocket.SendConnectionInitAsync(ct);

                // assert
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.TooManyInitAttempts, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Connect_Accept_Ping()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQL()
                        .ModifyServerOptions(o =>
                        {
                            o.Sockets.ConnectionInitializationTimeout =
                                TimeSpan.FromMilliseconds(1000);
                            o.Sockets.KeepAliveInterval = TimeSpan.FromMilliseconds(150);
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
                Assert.Equal(Messages.Ping, message.RootElement.GetProperty(MessageProperties.Type).GetString());
            });

    [Fact]
    public Task No_ConnectionInit_Timeout()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(
                    configureServices: s => s
                        .AddGraphQL()
                        .ModifyServerOptions(o =>
                        {
                            o.Sockets.ConnectionInitializationTimeout =
                                TimeSpan.FromMilliseconds(50);
                            o.Sockets.KeepAliveInterval = TimeSpan.FromMilliseconds(150);
                        }));
                var client = CreateWebSocketClient(testServer);

                // act
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // assert
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(
                    CloseReasons.ConnectionInitWaitTimeout,
                    (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Connect_With_Auth_Accept()
        => RunTest(
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
                await webSocket.SendConnectionInitAsync(new() { ["token"] = "abc " }, ct);

                // assert
                var message = await webSocket.ReceiveServerMessageAsync(ct);
                Assert.NotNull(message);
                Assert.Equal(Messages.ConnectionAccept, message.RootElement.GetProperty(MessageProperties.Type).GetString());
            });

    [Fact]
    public Task Send_Connect_With_Auth_Reject()
        => RunTest(
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
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(CloseReasons.Unauthorized, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Connect_Accept_Explicit_Route()
        => RunTest(
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
                Assert.Equal("connection_ack", message.RootElement.GetProperty("type").GetString());
            });

    [Fact]
    public Task Send_Connect_Accept_Explicit_Route_Explicit_Path()
        => RunTest(
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
                Assert.Equal("connection_ack", message.RootElement.GetProperty("type").GetString());
            });

    [Fact]
    public Task Connect_With_Invalid_Protocol()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
                var client = testServer.CreateWebSocketClient();

                // act
                client.ConfigureRequest = r => r.Headers.SecWebSocketProtocol = "foo";
                using var socket = await client.ConnectAsync(SubscriptionUri, ct);

                // assert
                await WaitForServerClose(socket, ct);
                Assert.True(socket.CloseStatus.HasValue);
                Assert.Equal(ProtocolError, socket.CloseStatus!.Value);
            });

    [Fact]
    public Task Subscribe_ReceiveDataOnMutation()
    {
        var snapshot = new Snapshot();

        return RunTest(
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
                    output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                const string subscriptionId = "abc";

                // act
                await SendSubscribeAndWaitForRegistrationAsync(
                    webSocket,
                    subscriptionId,
                    payload,
                    diagnostics.WaitForSubscribedAsync,
                    ct);

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

                // assert
                var message = await WaitForMessage(webSocket, Messages.Next, ct);
                Assert.NotNull(message);
                await snapshot.Add(message).MatchAsync(ct);
            });
    }

    [Fact]
    public Task Subscribe_With_PersistedQuery_Extension_Only_Works()
        => RunTest(
            async ct =>
            {
                // arrange
                var storage = new OperationStorage();
                var hashProvider = new MD5DocumentHashProvider(HashFormat.Base64);
                var diagnostics = new SubscriptionTestDiagnostics();
                const string query = "subscription { onReview(episode: NEW_HOPE) { stars } }";
                var hash = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(query)).Value;
                storage.AddOperation(hash, query);

                using var testServer = CreateStarWarsServer(
                    configureServices: services => services
                        .AddGraphQLServer()
                        .AddMD5DocumentHashProvider(HashFormat.Base64)
                        .AddDiagnosticEventListener(_ => diagnostics)
                        .ConfigureSchemaServices(c => c.AddSingleton<IOperationDocumentStorage>(storage)),
                    output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var subscribeMessage = JsonSerializer.Serialize(
                    new
                    {
                        type = "subscribe",
                        id = "abc",
                        payload = new
                        {
                            extensions = new Dictionary<string, object?>
                            {
                                ["persistedQuery"] = new Dictionary<string, object?>
                                {
                                    ["version"] = 1,
                                    [hashProvider.Name] = hash
                                }
                            }
                        }
                    });

                // act
                await webSocket.SendMessageAsync(subscribeMessage, ct);
                await diagnostics.WaitForSubscribedAsync(ct);

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

                // assert
                var message = await WaitForMessage(webSocket, Messages.Next, ct);
                Assert.NotNull(message);
            });

    [Fact]
    public Task Subscribe_Id_Not_Unique()
    {
        return RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                const string subscriptionId = "abc";

                // act
                await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);
                await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

                // assert
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.SubscriberNotUnique, (int)webSocket.CloseStatus!.Value);
            });
    }

    [Fact]
    public Task Send_Subscribe_No_Auth_Close()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                const string subscriptionId = "abc";
                await webSocket.SendSubscribeAsync(subscriptionId, payload, ct);

                // assert
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.Unauthorized, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Subscribe_No_Id()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act

                await webSocket.SendMessageAsync("{ \"type\": \"subscribe\" }", ct);

                // assert
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.ProtocolError, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Subscribe_Empty_Id()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act

                await webSocket.SendMessageAsync("{ \"type\": \"subscribe\", \"id\": \"\" }", ct);

                // assert
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue);
                Assert.Equal(CloseReasons.ProtocolError, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public async Task Send_Subscribe_Complete()
    {
        await RunTest(
            async ct =>
            {
                // arrange
                var diagnostics = new SubscriptionTestDiagnostics();
                using var testServer = CreateStarWarsServer(
                    configureServices: services
                        => services
                            .AddGraphQL()
                            .AddDiagnosticEventListener(_ => diagnostics),
                    output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                const string subscriptionId = "abc";
                await SendSubscribeAndWaitForRegistrationAsync(
                    webSocket,
                    subscriptionId,
                    payload,
                    diagnostics.WaitForSubscribedAsync,
                    ct);

                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query =
                            """
                            mutation {
                                createReview(
                                    episode: NEW_HOPE
                                    review: { commentary: "foo", stars: 5 }
                                ) {
                                    stars
                                }
                            }
                            """
                    });

                await WaitForMessage(webSocket, Messages.Next, ct);

                // act
                await webSocket.SendCompleteAsync(subscriptionId, ct);

                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query =
                            """
                            mutation {
                                createReview(
                                    episode: NEW_HOPE
                                    review: { commentary: "foo", stars: 5 }
                                ) {
                                    stars
                                }
                            }
                            """
                    });

                // assert
                Assert.True(await AssertNoMessage(webSocket, Messages.Next, ct));
                await diagnostics.WaitForUnsubscribeAsync(ct);
                await diagnostics.WaitForCloseAsync(ct);
            });
    }

    [Fact]
    public async Task Send_Subscribe_Complete_From_Server()
    {
        await RunTest(
            async ct =>
            {
                // arrange
                var diagnostics = new SubscriptionTestDiagnostics();
                using var testServer = CreateStarWarsServer(
                    configureServices: services
                        => services
                            .AddGraphQL()
                            .AddDiagnosticEventListener(_ => diagnostics),
                    output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");
                const string subscriptionId = "abc";
                await SendSubscribeAndWaitForRegistrationAsync(
                    webSocket,
                    subscriptionId,
                    payload,
                    diagnostics.WaitForSubscribedAsync,
                    ct);

                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query =
                            """
                            mutation {
                                createReview(
                                    episode: NEW_HOPE
                                    review: { commentary: "foo", stars: 5 }
                                ) {
                                    stars
                                }
                            }
                            """
                    });

                await WaitForMessage(webSocket, Messages.Next, ct);

                // act
                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query = @"
                    mutation {
                        complete(episode:NEW_HOPE)
                    }"
                    });

                // assert
                await WaitForMessage(webSocket, Messages.Complete, ct);
                await diagnostics.WaitForUnsubscribeAsync(ct);
                await diagnostics.WaitForCloseAsync(ct);
            });
    }

    [Fact]
    public async Task Send_Subscribe_100x_Complete_From_Server()
    {
        await RunTest(
            async ct =>
            {
                // arrange
                var diagnostics = new SubscriptionTestDiagnostics();
                using var testServer = CreateStarWarsServer(
                    configureServices: services
                        => services
                            .AddGraphQL()
                            .AddDiagnosticEventListener(_ => diagnostics),
                    output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");

                var stopwatch = Stopwatch.StartNew();

                for (var i = 0; i < 100; i++)
                {
                    await webSocket.SendSubscribeAsync(i.ToString(), payload, ct);
                }

                await diagnostics.WaitForSubscribedAsync(100, ct);

                output.WriteLine($"Subscribed in {stopwatch.ElapsedMilliseconds}ms");

                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query =
                            """
                            mutation {
                                createReview(
                                    episode: NEW_HOPE
                                    review: { commentary: "foo", stars: 5 }
                                ) {
                                    stars
                                }
                            }
                            """
                    });

                for (var i = 0; i < 100; i++)
                {
                    await WaitForMessage(webSocket, Messages.Next, ct);
                }

                // act
                await testServer.SendPostRequestAsync(
                    new ClientQueryRequest
                    {
                        Query = "mutation { complete(episode:NEW_HOPE) }"
                    });

                // assert
                for (var i = 0; i < 100; i++)
                {
                    await WaitForMessage(webSocket, Messages.Complete, ct);
                }

                await diagnostics.WaitForUnsubscribeAsync(ct);
                await diagnostics.WaitForCloseAsync(ct);
            });
    }

    [Fact]
    public Task Send_Subscribe_SyntaxError()
    {
        var snapshot = new Snapshot();

        return RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
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

        return RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
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

        return RunTest(
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

        return RunTest(
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
                    new Dictionary<string, object?> { ["abc"] = "def" },
                    ct);

                // assert
                var message = await WaitForMessage(webSocket, Messages.Pong, ct);
                Assert.NotNull(message);
                await snapshot.Add(message).MatchAsync(ct);
            });
    }

    [Fact]
    public Task Send_Pong()
        => RunTest(
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

        return RunTest(
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
                    new Dictionary<string, object?> { ["abc"] = "def" },
                    ct);

                // assert
                await WaitForConditions(() => interceptor.OnPongInvoked, ct);
                await snapshot.Add(interceptor.Payload).MatchAsync(ct);
            });
    }

    [Fact]
    public Task Send_Invalid_Message_String()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                await webSocket.SendMessageAsync("hello", ct);

                // assert
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(InternalServerError, webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Invalid_Message_No_Type()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                await webSocket.SendMessageAsync("{ }", ct);

                // assert
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(CloseReasons.ProtocolError, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Invalid_Message_Invalid_Type()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                // act
                await webSocket.SendMessageAsync("{ \"type\": \"abc\" }", ct);

                // assert
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(CloseReasons.ProtocolError, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Send_Invalid_Message_Not_An_Object()
        => RunTest(
            async ct =>
            {
                // arrange
                using var testServer = CreateStarWarsServer(output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await client.ConnectAsync(SubscriptionUri, ct);

                // act
                await webSocket.SendMessageAsync("[]", ct);

                // assert
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(CloseReasons.ProtocolError, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Normal_Closure()
        => RunTest(
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

                // act & assert
                await WaitForServerClose(webSocket, ct);
                Assert.True(webSocket.CloseStatus.HasValue, "Connection is closed.");
                Assert.Equal(CloseReasons.Unauthorized, (int)webSocket.CloseStatus!.Value);
            });

    [Fact]
    public Task Subscribe_Cancel()
    {
        return RunTest(
            async ct =>
            {
                // arrange
                var diagnostics = new SubscriptionTestDiagnostics();
                var subscriptionRequest = new OperationRequest(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");

                using var testServer = CreateStarWarsServer(
                    configureServices: services => services
                        .AddGraphQL()
                        .AddDiagnosticEventListener(_ => diagnostics),
                    output: output);
                var webSocketClient = CreateWebSocketClient(testServer);
                using var webSocket = await webSocketClient.ConnectAsync(SubscriptionUri, ct);

                var client = await SocketClient.ConnectAsync(webSocket, ct);

                // act
                var socketResult = await client.ExecuteAsync(subscriptionRequest, ct);
                await diagnostics.WaitForSubscribedAsync(ct);

                socketResult.Dispose();
                await diagnostics.WaitForUnsubscribeAsync(ct);

                socketResult = await client.ExecuteAsync(subscriptionRequest, ct);
                await diagnostics.WaitForSubscribedAsync(2, ct);

                socketResult.Dispose();
                await diagnostics.WaitForUnsubscribeAsync(2, ct);
            });
    }

    [Fact]
    public Task Subscribe_ReceiveDataOnMutation_StripNull()
        => RunTest(
            async ct =>
            {
                // arrange
                var diagnostics = new SubscriptionTestDiagnostics();
                using var testServer = CreateStarWarsServer(
                    configureServices: c =>
                        c.AddGraphQL()
                            .AddDiagnosticEventListener(_ => diagnostics)
                            .AddWebSocketPayloadFormatter(
                                _ => new DefaultWebSocketPayloadFormatter(
                                    new WebSocketPayloadFormatterOptions
                                    {
                                        Json = new JsonResultFormatterOptions
                                        {
                                            NullIgnoreCondition = JsonNullIgnoreCondition.FieldsAndLists
                                        }
                                    })),
                    output: output);
                var client = CreateWebSocketClient(testServer);
                using var webSocket = await ConnectToServerAsync(client, ct);

                var payload = new SubscribePayload(
                    "subscription { onReview(episode: NEW_HOPE) { stars, commentary } }");
                const string subscriptionId = "abc";

                // act
                await SendSubscribeAndWaitForRegistrationAsync(
                    webSocket,
                    subscriptionId,
                    payload,
                    diagnostics.WaitForSubscribedAsync,
                    ct);

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

                // assert
                var message = await WaitForMessage(webSocket, Messages.Next, ct);
                Assert.NotNull(message);
                var messagePayload = message.RootElement.GetProperty("payload");
                var messageData = messagePayload.GetProperty("data");
                var messageOnReview = messageData.GetProperty("onReview");
                Assert.False(messageOnReview.TryGetProperty("commentary", out _));
            });

    private sealed class OperationStorage : IOperationDocumentStorage
    {
        private readonly Dictionary<string, OperationDocument> _cache =
            new(StringComparer.Ordinal);

        public ValueTask<IOperationDocument?> TryReadAsync(
            OperationDocumentId documentId,
            CancellationToken cancellationToken = default)
            => _cache.TryGetValue(documentId.Value, out var value)
                ? new ValueTask<IOperationDocument?>(value)
                : new ValueTask<IOperationDocument?>(default(IOperationDocument));

        public ValueTask SaveAsync(
            OperationDocumentId documentId,
            IOperationDocument document,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public void AddOperation(string key, string sourceText)
        {
            var doc = new OperationDocument(Utf8GraphQLParser.Parse(sourceText));
            _cache.Add(key, doc);
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
            public string? Token { get; init; }
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
            var payload = pingMessage.Payload?.Deserialize<Dictionary<string, string?>>();
            var responsePayload = new Dictionary<string, object?> { ["touched"] = true };

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
            Payload = pongMessage.Payload?.Deserialize<Dictionary<string, string?>>();
            return base.OnPongAsync(session, pongMessage, cancellationToken);
        }
    }

    public sealed class SubscriptionTestDiagnostics : SubscriptionDiagnosticEventsListener
    {
        private readonly object _sync = new();
        private readonly List<(int Count, TaskCompletionSource Signal)> _subscribedSignals = [];
        private readonly List<(int Count, TaskCompletionSource Signal)> _unsubscribeSignals = [];
        private readonly List<(int Count, TaskCompletionSource Signal)> _closeSignals = [];
        private int _subscribed;
        private int _unsubscribeCount;
        private int _closeCount;

        public int Subscribed => _subscribed;

        public bool UnsubscribeInvoked { get; private set; }

        public bool CloseInvoked { get; private set; }

        public override void SubscribeSuccess(string topicName)
        {
            var subscribed = Interlocked.Increment(ref _subscribed);
            SignalCompleted(_subscribedSignals, subscribed);
        }

        public override void Unsubscribe(string topicName, int shard, int subscribers)
        {
            UnsubscribeInvoked = true;
            var unsubscribeCount = Interlocked.Increment(ref _unsubscribeCount);
            SignalCompleted(_unsubscribeSignals, unsubscribeCount);
        }

        public override void Close(string topicName)
        {
            CloseInvoked = true;
            var closeCount = Interlocked.Increment(ref _closeCount);
            SignalCompleted(_closeSignals, closeCount);
        }

        public Task WaitForSubscribedAsync(CancellationToken cancellationToken)
            => WaitForSubscribedAsync(1, cancellationToken);

        public Task WaitForSubscribedAsync(int count, CancellationToken cancellationToken)
            => WaitForSignalAsync(_subscribedSignals, count, _subscribed, cancellationToken);

        public Task WaitForUnsubscribeAsync(CancellationToken cancellationToken)
            => WaitForUnsubscribeAsync(1, cancellationToken);

        public Task WaitForUnsubscribeAsync(int count, CancellationToken cancellationToken)
            => WaitForSignalAsync(_unsubscribeSignals, count, _unsubscribeCount, cancellationToken);

        public Task WaitForCloseAsync(CancellationToken cancellationToken)
            => WaitForCloseAsync(1, cancellationToken);

        public Task WaitForCloseAsync(int count, CancellationToken cancellationToken)
            => WaitForSignalAsync(_closeSignals, count, _closeCount, cancellationToken);

        private Task WaitForSignalAsync(
            List<(int Count, TaskCompletionSource Signal)> signals,
            int count,
            int currentCount,
            CancellationToken cancellationToken)
        {
            if (currentCount >= count)
            {
                return Task.CompletedTask;
            }

            TaskCompletionSource signal;

            lock (_sync)
            {
                if (GetCurrentCount(signals) >= count)
                {
                    return Task.CompletedTask;
                }

                signal = new(TaskCreationOptions.RunContinuationsAsynchronously);
                signals.Add((count, signal));
            }

            return signal.Task.WaitAsync(cancellationToken);
        }

        private int GetCurrentCount(List<(int Count, TaskCompletionSource Signal)> signals)
            => ReferenceEquals(signals, _subscribedSignals)
                ? _subscribed
                : ReferenceEquals(signals, _unsubscribeSignals)
                    ? _unsubscribeCount
                    : _closeCount;

        private void SignalCompleted(List<(int Count, TaskCompletionSource Signal)> signals, int count)
        {
            TaskCompletionSource[] ready;

            lock (_sync)
            {
                ready = [.. signals.Where(t => t.Count <= count).Select(t => t.Signal)];
                signals.RemoveAll(t => t.Count <= count);
            }

            foreach (var signal in ready)
            {
                signal.TrySetResult();
            }
        }
    }
}
