using HotChocolate.Fusion.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class SubscriptionPoliciesWebSocketTests : SubscriptionPoliciesTransportTestBase
{
    [Fact]
    public async Task ReceiveAsync_Should_SendTheDenialAsPerIdError_When_TheSubscribeFieldIsDeniedForAnonymous()
    {
        // arrange
        using var gateway = await CreateSubscriptionGatewayAsync(DenyHandling.Error);
        using var socket = await ConnectAsync(gateway, user: null, expiry: null);

        // act
        await socket.SendSubscribeAsync("1", "subscription { secret { id } }");
        var denied = await socket.ReceiveAsync();
        await socket.SendSubscribeAsync("2", "subscription { changed { id tag } }");
        await gateway.Feed.WaitForOpenedAsync(1);
        gateway.Feed.Publish(Event);
        var allowed = await socket.ReceiveAsync();

        // assert
        new[] { denied, allowed }.MatchInlineSnapshots(
            [
                """
                {
                  "id": "1",
                  "type": "error",
                  "payload": [
                    {
                      "message": "The current user is not authenticated.",
                      "extensions": {
                        "code": "AUTH_NOT_AUTHENTICATED"
                      }
                    }
                  ]
                }
                """,
                """
                {
                  "id": "2",
                  "type": "next",
                  "payload": {
                    "data": {
                      "changed": {
                        "id": "1",
                        "tag": "tag"
                      }
                    }
                  }
                }
                """
            ]);
    }

    [Fact]
    public async Task ReceiveAsync_Should_SendTheDenialAsPerIdError_When_TheSubscribeFieldIsDeniedForAuthenticated()
    {
        // arrange
        using var gateway = await CreateSubscriptionGatewayAsync(DenyHandling.Error);
        using var socket = await ConnectAsync(gateway, Member, expiry: null);

        // act
        await socket.SendSubscribeAsync("1", "subscription { scoped { id } }");
        var denied = await socket.ReceiveAsync();
        await socket.SendSubscribeAsync("2", "subscription { changed { id tag } }");
        await gateway.Feed.WaitForOpenedAsync(1);
        gateway.Feed.Publish(Event);
        var allowed = await socket.ReceiveAsync();

        // assert
        new[] { denied, allowed }.MatchInlineSnapshots(
            [
                """
                {
                  "id": "1",
                  "type": "error",
                  "payload": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ]
                }
                """,
                """
                {
                  "id": "2",
                  "type": "next",
                  "payload": {
                    "data": {
                      "changed": {
                        "id": "1",
                        "tag": "tag"
                      }
                    }
                  }
                }
                """
            ]);
    }

    [Fact]
    public async Task ReceiveAsync_Should_SendTheRefusalAsPerIdError_When_TheTokenExpiredBeforeTheSubscription()
    {
        // arrange
        using var gateway = await CreateSubscriptionGatewayAsync(DenyHandling.Error);
        var expiredAt = FormatExpiry(gateway.Time.GetUtcNow().AddMinutes(-1));
        using var socket = await ConnectAsync(gateway, Member, expiredAt);

        // act
        await socket.SendSubscribeAsync("1", ChangedSubscription);
        var refused = await socket.ReceiveAsync();

        // assert
        Snapshot.Create()
            .Add(refused, "Message")
            .Add(gateway.Feed.Opened, "Source Schema Subscriptions")
            .Add(gateway.Audit.Scopes.ToArray(), "Scopes")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ReceiveAsync_Should_CloseTheConnectionAsUnauthorized_When_TheTokenExpires()
    {
        // arrange
        using var gateway = await CreateSubscriptionGatewayAsync(DenyHandling.Error);
        var expiresAt = FormatExpiry(gateway.Time.GetUtcNow().AddMinutes(10));
        using var socket = await ConnectAsync(gateway, Member, expiresAt);
        await socket.SendSubscribeAsync("1", ChangedSubscription);
        await gateway.Feed.WaitForOpenedAsync(1);
        gateway.Feed.Publish(Event);
        var first = await socket.ReceiveAsync();

        // act
        gateway.Time.Advance(TimeSpan.FromMinutes(10));
        var closed = await socket.ReceiveAsync();

        // assert
        new[] { first, closed }.MatchInlineSnapshots(
            [
                """
                {
                  "id": "1",
                  "type": "next",
                  "payload": {
                    "data": {
                      "changed": {
                        "id": "1",
                        "name": "name",
                        "tag": "tag"
                      }
                    }
                  }
                }
                """,
                """
                close 4401 Unauthorized
                """
            ]);
    }

    [Fact]
    public async Task InitializeAsync_Should_AnswerAccordingToTheInterceptor_When_ConnectionInitIsSent()
    {
        // arrange
        using var gateway = await CreateSubscriptionGatewayAsync(
            DenyHandling.Error,
            builder => builder.AddSocketSessionInterceptor<RejectingSocketSessionInterceptor>());
        using var anonymous = await SubscriptionWebSocketClient.ConnectAsync(
            gateway.Gateway.CreateWebSocketClient(),
            user: null,
            expiry: null);
        using var member = await SubscriptionWebSocketClient.ConnectAsync(
            gateway.Gateway.CreateWebSocketClient(),
            Member,
            expiry: null);
        using var reader = await SubscriptionWebSocketClient.ConnectAsync(
            gateway.Gateway.CreateWebSocketClient(),
            Reader,
            expiry: null);

        // act
        var anonymousAnswer = await anonymous.InitializeAsync();
        var memberAnswer = await member.InitializeAsync();
        var readerAnswer = await reader.InitializeAsync();

        // assert
        new[] { anonymousAnswer, memberAnswer, readerAnswer }.MatchInlineSnapshots(
            [
                """
                close 4401 Unauthenticated
                """,
                """
                close 4403 Forbidden
                """,
                """
                {
                  "type": "connection_ack"
                }
                """
            ]);
    }

    private static async Task<SubscriptionWebSocketClient> ConnectAsync(
        SubscriptionGateway gateway,
        string? user,
        string? expiry)
    {
        var socket = await SubscriptionWebSocketClient.ConnectAsync(
            gateway.Gateway.CreateWebSocketClient(),
            user,
            expiry);
        await socket.InitializeAsync();

        return socket;
    }
}
