using HotChocolate.Fusion.Policies;

namespace HotChocolate.Fusion;

public class SubscriptionPoliciesSseTests : SubscriptionPoliciesTransportTestBase
{
    [Fact]
    public async Task StartAsync_Should_RefuseWithUnauthorized_When_TheSubscribeFieldIsDeniedForAnonymous()
    {
        // arrange
        using var gateway = await CreateSubscriptionGatewayAsync(DenyHandling.Error);
        using var client = gateway.Gateway.CreateClient();

        // act
        using var sse = await SubscriptionSseClient.StartAsync(client, "subscription { secret { id } }");

        // assert
        await SnapshotRefusalAsync(sse, gateway);
    }

    [Fact]
    public async Task StartAsync_Should_RefuseWithForbidden_When_TheSubscribeFieldIsDeniedForAuthenticated()
    {
        // arrange
        using var gateway = await CreateSubscriptionGatewayAsync(DenyHandling.Error);
        using var client = gateway.Gateway.CreateClient();

        // act
        using var sse = await SubscriptionSseClient.StartAsync(
            client,
            "subscription { scoped { id } }",
            Member);

        // assert
        await SnapshotRefusalAsync(sse, gateway);
    }

    [Fact]
    public async Task StartAsync_Should_RefuseWithUnauthorized_When_TheTokenExpiredBeforeTheSubscription()
    {
        // arrange
        using var gateway = await CreateSubscriptionGatewayAsync(DenyHandling.Error);
        using var client = gateway.Gateway.CreateClient();
        var expiredAt = FormatExpiry(gateway.Time.GetUtcNow().AddMinutes(-1));

        // act
        using var sse = await SubscriptionSseClient.StartAsync(client, ChangedSubscription, Member, expiredAt);

        // assert
        await SnapshotRefusalAsync(sse, gateway);
    }

    [Fact]
    public async Task ReadEventAsync_Should_StreamNullWithErrorOnEveryEvent_When_ANullableFieldIsDeniedAtSubscribe()
    {
        // arrange
        using var gateway = await CreateSubscriptionGatewayAsync(DenyHandling.Error, DenyLivePolicy);
        using var client = gateway.Gateway.CreateClient();
        using var sse = await StartSseAsync(gateway, client, ChangedSubscription, Member, null);

        // act
        gateway.Feed.Publish(Event);
        var first = await sse.ReadEventAsync();
        gateway.Feed.Publish(Event);
        var second = await sse.ReadEventAsync();

        // assert
        new[] { first, second }.MatchInlineSnapshots(
            [
                """
                event: next
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "changed",
                        "name"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": {
                    "changed": {
                      "id": "1",
                      "name": null,
                      "tag": "tag"
                    }
                  }
                }
                """,
                """
                event: next
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "changed",
                        "name"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": {
                    "changed": {
                      "id": "1",
                      "name": null,
                      "tag": "tag"
                    }
                  }
                }
                """
            ]);
    }

    [Fact]
    public async Task ReadEventAsync_Should_StreamDataNullWithErrorOnEveryEvent_When_ANonNullFieldIsDeniedAtSubscribe()
    {
        // arrange
        using var gateway = await CreateSubscriptionGatewayAsync(DenyHandling.Error, DenyLivePolicy);
        using var client = gateway.Gateway.CreateClient();
        using var sse = await StartSseAsync(gateway, client, "subscription { required { id code } }", Member, null);

        // act
        gateway.Feed.Publish(Event);
        var first = await sse.ReadEventAsync();
        gateway.Feed.Publish(Event);
        var second = await sse.ReadEventAsync();

        // assert
        new[] { first, second }.MatchInlineSnapshots(
            [
                """
                event: next
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "required",
                        "code"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": null
                }
                """,
                """
                event: next
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "path": [
                        "required",
                        "code"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": null
                }
                """
            ]);
    }

    [Fact]
    public async Task ReadToEndAsync_Should_CompleteTheStream_When_TheTokenExpires()
    {
        // arrange
        using var gateway = await CreateSubscriptionGatewayAsync(DenyHandling.Error);
        using var client = gateway.Gateway.CreateClient();
        var expiresAt = FormatExpiry(gateway.Time.GetUtcNow().AddMinutes(10));
        using var sse = await StartSseAsync(gateway, client, ChangedSubscription, Member, expiresAt);
        gateway.Feed.Publish(Event);
        var first = await sse.ReadEventAsync();

        // act
        gateway.Time.Advance(TimeSpan.FromMinutes(10));
        var remaining = await sse.ReadToEndAsync();

        // assert
        string?[] events = [first, .. remaining];
        events.MatchInlineSnapshots(
            [
                """
                event: next
                {
                  "data": {
                    "changed": {
                      "id": "1",
                      "name": "name",
                      "tag": "tag"
                    }
                  }
                }
                """,
                """
                event: complete
                """
            ]);
    }

    private static async Task SnapshotRefusalAsync(SubscriptionSseClient sse, SubscriptionGateway gateway)
    {
        Snapshot.Create()
            .Add($"{(int)sse.StatusCode} {sse.StatusCode}", "Status")
            .Add(sse.Challenge ?? "none", "Challenge")
            .Add((await sse.ReadToEndAsync()).ToArray(), "Events")
            .Add(gateway.Feed.Opened, "Source Schema Subscriptions")
            .Add(gateway.Audit.Scopes.ToArray(), "Scopes")
            .MatchMarkdownSnapshot();
    }
}
