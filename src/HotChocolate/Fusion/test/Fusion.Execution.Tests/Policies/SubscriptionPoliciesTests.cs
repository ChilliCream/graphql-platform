using System.Collections.Immutable;
using System.Globalization;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Policies.Auditing;
using HotChocolate.Fusion.Policies.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HotChocolate.Fusion.Policies;

public class SubscriptionPoliciesTests : PolicyExecutionTestBase
{
    private const string Schema =
        """
        type Query {
          open: Int
        }

        type Subscription {
          changed: Item
          secret: String @authenticated
          scoped: String @requiresScopes(scopes: [["read"]])
        }

        type Item {
          id: ID!
          name: String @policy(policies: [["live"]])
          tag: String
        }
        """;

    private const string Event =
        """
        { "changed": { "id": "1", "name": "n", "tag": "t" } }
        """;

    private const string Subscription = "subscription { changed { id name tag } }";

    [Fact]
    public async Task ExecuteAsync_Should_RefuseWithUnauthorized_When_SubscribeFieldIsDeniedForAnonymous()
    {
        // arrange
        var client = new PolicyTestClient(Event);
        var executor = await CreateSubscriptionExecutorAsync(client, policies => policies.Allow("live"));

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("subscription { secret }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        Snapshot.Create()
            .Add(result, "Response")
            .Add(DescribeTransport(result), "Transport")
            .Add(client.SubscribeCalls, "Source Schema Subscriptions")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_RefuseWithForbidden_When_SubscribeFieldIsDeniedForAuthenticated()
    {
        // arrange
        var client = new PolicyTestClient(Event);
        var executor = await CreateSubscriptionExecutorAsync(client, policies => policies.Allow("live"));

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest("subscription { scoped }", Authenticated()).Build(),
            TestContext.Current.CancellationToken);

        // assert
        Snapshot.Create()
            .Add(result, "Response")
            .Add(DescribeTransport(result), "Transport")
            .Add(client.SubscribeCalls, "Source Schema Subscriptions")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_RefuseWithUnauthorized_When_TheTokenExpiredBeforeTheSubscription()
    {
        // arrange
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddHours(1));
        var client = new PolicyTestClient(Event);
        var audit = new RecordingAuditProvider(client);
        var executor = await CreateSubscriptionExecutorAsync(
            client,
            policies => policies.Allow("live"),
            builder => builder.ConfigureSchemaServices(
                (_, sc) =>
                {
                    sc.AddSingleton<TimeProvider>(time);
                    sc.AddSingleton<IAuditProvider>(audit);
                }));
        var user = Authenticated(CreateExpiryClaim(time.GetUtcNow().AddMinutes(-1)));

        // act
        await using var result = await executor.ExecuteAsync(
            CreateRequest(Subscription, user).Build(),
            TestContext.Current.CancellationToken);

        // assert
        Snapshot.Create()
            .Add(result, "Response")
            .Add(DescribeTransport(result), "Transport")
            .Add(client.SubscribeCalls, "Source Schema Subscriptions")
            .Add(audit.Scopes.Select(Format).ToArray(), "Scopes")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ReadResultsAsync_Should_RenderTheDenialErrorOnEveryEvent_When_FieldIsDeniedAtSubscribeInErrorMode()
    {
        // arrange
        var client = new PolicyTestClient(Event);
        var recorder = new InMemoryPolicyRecorder();
        var executor = await CreateExecutorAsync(
            Schema,
            client,
            recorder,
            policies => policies.Deny("live"),
            builder => builder.ModifyPolicyOptions(options => options.DenyHandling = DenyHandling.Error));
        await using var result = await executor.ExecuteAsync(
            CreateRequest(Subscription, Authenticated()).Build(),
            TestContext.Current.CancellationToken);
        await using var events = ReadEvents(result);

        // act
        client.Publish(Event);
        var first = await ReadNextAsync(events);
        client.Publish(Event);
        var second = await ReadNextAsync(events);

        // assert
        new[] { first, second }.MatchInlineSnapshots(
            [
                """
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
                      "tag": "t"
                    }
                  }
                }
                """,
                """
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
                      "tag": "t"
                    }
                  }
                }
                """
            ]);
        Assert.Single(recorder.Records);
    }

    [Fact]
    public async Task ReadResultsAsync_Should_EndTheStreamAndCommitAScope_When_TheTokenExpires()
    {
        // arrange
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddHours(1));
        var client = new PolicyTestClient(Event);
        var audit = new RecordingAuditProvider(client);
        var executor = await CreateSubscriptionExecutorAsync(
            client,
            policies => policies.Allow("live"),
            builder => builder.ConfigureSchemaServices(
                (_, sc) =>
                {
                    sc.AddSingleton<TimeProvider>(time);
                    sc.AddSingleton<IAuditProvider>(audit);
                }));
        var user = Authenticated(CreateExpiryClaim(time.GetUtcNow().AddMinutes(10)));
        await using var result = await executor.ExecuteAsync(
            CreateRequest(Subscription, user).Build(),
            TestContext.Current.CancellationToken);
        await using var events = ReadEvents(result);

        // act
        client.Publish(Event);
        var first = await ReadNextAsync(events);
        time.Advance(TimeSpan.FromMinutes(10));
        var hasNext = await events.MoveNextAsync();

        // assert
        Snapshot.Create()
            .Add(first, "Event")
            .Add(hasNext, "Has Next")
            .Add(GetRequestedStatusCode(result), "Requested Status Code")
            .Add(audit.Scopes.Select(Format).ToArray(), "Scopes")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ReadResultsAsync_Should_Throw_When_TheExpiryScopeFailsToCommit()
    {
        // arrange
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch.AddHours(1));
        var client = new PolicyTestClient(Event);
        var audit = new RecordingAuditProvider(client);
        var executor = await CreateSubscriptionExecutorAsync(
            client,
            policies => policies.Allow("live"),
            builder => builder.ConfigureSchemaServices(
                (_, sc) =>
                {
                    sc.AddSingleton<TimeProvider>(time);
                    sc.AddSingleton<IAuditProvider>(audit);
                }));
        var user = Authenticated(CreateExpiryClaim(time.GetUtcNow().AddMinutes(10)));
        await using var result = await executor.ExecuteAsync(
            CreateRequest(Subscription, user).Build(),
            TestContext.Current.CancellationToken);
        await using var events = ReadEvents(result);
        var expected = new InvalidOperationException("commit");
        audit.CommitFailure = expected;
        time.Advance(TimeSpan.FromMinutes(10));

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await events.MoveNextAsync());

        // assert
        Assert.Same(expected, exception);
        Snapshot.Create()
            .Add(GetRequestedStatusCode(result), "Requested Status Code")
            .Add(audit.Scopes.Select(Format).ToArray(), "Scopes")
            .MatchMarkdownSnapshot();
    }

    private static Task<IRequestExecutor> CreateSubscriptionExecutorAsync(
        PolicyTestClient client,
        Action<InMemoryPolicyBuilder> policies,
        Action<IFusionGatewayBuilder>? configure = null)
        => CreateExecutorAsync(Schema, client, new InMemoryPolicyRecorder(), policies, configure);

    private static object? GetRequestedStatusCode(IExecutionResult result)
        => result.ContextData.GetValueOrDefault(ExecutionContextData.HttpStatusCode);

    private static IAsyncEnumerator<OperationResult> ReadEvents(IExecutionResult result)
        => ((ResponseStream)result).ReadResultsAsync().GetAsyncEnumerator(TestContext.Current.CancellationToken);

    private static async Task<string> ReadNextAsync(IAsyncEnumerator<OperationResult> events)
    {
        Assert.True(await events.MoveNextAsync());

        await using var current = events.Current;

        return current.ToJson();
    }

    private static Claim CreateExpiryClaim(DateTimeOffset expiry)
        => new("exp", expiry.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));

    private static object DescribeTransport(IExecutionResult result)
    {
        var contextData = result.ExpectOperationResult().ContextData;

        return new
        {
            Status = contextData.GetValueOrDefault(ExecutionContextData.HttpStatusCode)?.ToString()
        };
    }

    private static string Format(RecordingAuditScope scope)
    {
        var entries = scope.Entries.Select(
            e => $"{e.Coordinate} @{e.DirectiveName}({e.PolicyName}) {e.Decision} {e.Reason ?? "-"}");

        return $"commits={scope.CommitCount} | {string.Join("; ", entries)} | failure={scope.FailureReason ?? "-"}";
    }
}
