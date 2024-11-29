using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.StarWars.Models;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using StrawberryShake.Transport.WebSockets.Protocols;
using static HotChocolate.StarWars.Types.Subscriptions;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsOnReviewSubCompletion;

public class StarWarsOnReviewSubCompletionTest(TestServerFactory serverFactory)
    : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Watch_StarWarsOnReviewSubCompletion_Test()
    {
        // arrange
        using var host = TestServerHelper.CreateServer(_ => { }, out var port);
        var topicEventSender = host.Services.GetRequiredService<ITopicEventSender>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddStarWarsOnReviewSubCompletionClient(
            profile: StarWarsOnReviewSubCompletionClientProfileKind.Default)
            .ConfigureHttpClient(
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
            .ConfigureWebSocketClient(
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        // act
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<IStarWarsOnReviewSubCompletionClient>();

        string? commentary = null;
        var completionTriggered = false;

        var sub = client.OnReviewSub.Watch();
        var session = sub.Subscribe(
            result => commentary = result.Data?.OnReview.Commentary,
            () => completionTriggered = true);

        var topic = Episode.NewHope;

        // try to send message 10 times
        // make sure the subscription connection is successful
        for(var times = 0; commentary is null && times < 10; times++)
        {
            await topicEventSender.SendAsync(
                $"{OnReview}_{topic}",
                new Review(stars: 1, commentary: "Commentary"));
            await Task.Delay(1_000);
        }

        // complete the topic of subscription from server
        await topicEventSender.CompleteAsync($"{OnReview}_{topic}");

        // waiting for completion message sent
        for (var times = 0; !completionTriggered && times < 10; times++)
        {
            await Task.Delay(1_000);
        }

        // assert
        Assert.True(completionTriggered);
        Assert.NotNull(commentary);

        session.Dispose();
    }

    [Fact]
    public async Task Watch_StarWarsOnReviewSubCompletionPassively_Test()
    {
        // arrange
        using var host = TestServerHelper.CreateServer(
            _ => { },
            out var port);
        var topicEventSender = host.Services.GetRequiredService<ITopicEventSender>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddStarWarsOnReviewSubCompletionClient(
            profile: StarWarsOnReviewSubCompletionClientProfileKind.Default)
            .ConfigureHttpClient(
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"))
            .ConfigureWebSocketClient(
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));

        serviceCollection.AddSingleton<SubscriptionSocketStateMonitor>();

        // act
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<IStarWarsOnReviewSubCompletionClient>();

        string? commentary = null;
        var completionTriggered = false;

        var sub = client.OnReviewSub.Watch();
        var session = sub.Subscribe(
            result => commentary = result.Data?.OnReview?.Commentary,
            () => completionTriggered = true);

        var topic = Episode.NewHope;

        // try to send message 10 times
        // make sure the subscription connection is successful
        for (var times = 0; commentary is null && times < 10; times++)
        {
            await topicEventSender.SendAsync(
                $"{OnReview}_{topic}",
                new Review(stars: 1, commentary: "Commentary"));
            await Task.Delay(1_000);
        }

        // simulate network error
        var monitor = services.GetRequiredService<SubscriptionSocketStateMonitor>();
        monitor.AbortSocket();

        //await host.StopAsync();

        // waiting for completion message sent
        for (var times = 0; !completionTriggered && times < 10; times++)
        {
            await Task.Delay(1_000);
        }

        // assert
        Assert.True(commentary is not null && completionTriggered);

        session.Dispose();
    }
}

public class SubscriptionSocketStateMonitor
{
    private const BindingFlags _bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly ISessionPool _sessionPool;
    private readonly Type _sessionPoolType;
    private readonly FieldInfo _sessionsField;

    private readonly FieldInfo _socketOperationsDictionaryField =
        typeof(Session).GetField("_operations", _bindingFlags)!;
    private readonly FieldInfo _socketOperationManagerField =
        typeof(SocketOperation).GetField("_manager", _bindingFlags)!;
    private readonly FieldInfo _socketProtocolField =
        typeof(Session).GetField("_socketProtocol", _bindingFlags)!;
    private readonly FieldInfo _protocolReceiverField =
        typeof(GraphQLWebSocketProtocol).GetField("_receiver", _bindingFlags)!;

    private Type? _sessionInfoType;
    private PropertyInfo? _sessionProperty;
    private Type? _receiverType;
    private FieldInfo? _receiverClientField;

    public SubscriptionSocketStateMonitor(ISessionPool sessionPool)
    {
        _sessionPool = sessionPool;
        _sessionPoolType = _sessionPool.GetType();
        _sessionsField = _sessionPoolType.GetField("_sessions", _bindingFlags)!;
    }

    public void AbortSocket()
    {
        var sessionInfos = (_sessionsField!.GetValue(_sessionPool)
            as System.Collections.IDictionary)!.Values;

        foreach (var sessionInfo in sessionInfos)
        {
            _sessionInfoType ??= sessionInfo.GetType();
            _sessionProperty ??= _sessionInfoType.GetProperty("Session")!;
            var session = _sessionProperty.GetValue(sessionInfo) as Session;
            var socketOperations = _socketOperationsDictionaryField
                .GetValue(session) as ConcurrentDictionary<string, SocketOperation>;

            foreach (var operation in socketOperations!)
            {
                var operationSession = _socketOperationManagerField.GetValue(operation.Value)
                    as Session;
                var protocol = _socketProtocolField.GetValue(operationSession)
                    as GraphQLWebSocketProtocol;

                var receiver = _protocolReceiverField.GetValue(protocol)!;

                _receiverType ??= receiver.GetType();
                _receiverClientField ??= _receiverType.GetField("_client", _bindingFlags)!;
                var client = _receiverClientField.GetValue(receiver) as ISocketClient;

                if (client!.IsClosed is false && client is WebSocketClient webSocketClient)
                {
                    var socket = typeof(WebSocketClient).GetField("_socket", _bindingFlags)!
                        .GetValue(webSocketClient) as ClientWebSocket;
                    socket!.Abort();
                }
            }
        }
    }
}
