using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Subscriptions.NATS;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using OperationRequest = HotChocolate.Transport.OperationRequest;
using OperationResult = HotChocolate.Transport.OperationResult;

namespace HotChocolate.Fusion;

/// <summary>
/// End-to-end coverage for a Federated Event Stream whose selected fields require an enrichment
/// lookup that the source schema rejects. Models the fusion-demo-sfo Reviews subgraph: a relay
/// Node <c>Review</c> (so <c>Review.id</c> is a global id and the enrichment runs through
/// <c>node(id:)</c>), an <c>@eventStream</c> message that only carries the id, and a client that
/// also selects <c>body</c>. When an event carries an id that is not a valid global id the lookup
/// errors; the subscription must surface that as a per-event error result (subgraph error plus
/// standard non-null propagation) and stay alive, not silently close.
/// </summary>
public class EventStreamEnrichmentErrorOverHttpTests : FusionTestBase
{
    private const string BrokerName = "nats";
    private const string Topic = "onCreateReview";
    private const string GatewayUrl = "http://localhost:5000/graphql";

    [Fact]
    public async Task Subscribe_Should_ReturnErrorAndStayAlive_When_EnrichmentLookupRejectsEventId()
    {
        // arrange
        await using var nats = await JetStreamNatsFixture.StartAsync();
        var stream = "S" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await CreateStreamAsync(nats.Url, stream, Topic, cts.Token);

        using var events = CreateSourceSchema(
            "EVENTS",
            b => b
                .AddQueryType<ReviewsApi.Query>()
                .AddSubscriptionType<ReviewsApi.Subscription>()
                .AddGlobalObjectIdentification(o => o.MarkNodeFieldAsLookup = true)
                .ModifyOptions(o => o.StrictValidation = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("EVENTS", events)],
            configureServices: services => services.AddNatsEventStreamBroker(
                BrokerName,
                o =>
                {
                    o.Url = nats.Url;
                    o.JetStream = new NatsJetStreamOptions { Stream = stream };
                }),
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            subscription {
              onCreateReview {
                review {
                  id
                  body
                }
                cursor
              }
            }
            """);

        // The encoded global id the second (valid) event publishes, matching what Review.id is.
        var schema = await events.Services.GetSchemaAsync("EVENTS", cts.Token);
        var validId = schema.Services
            .GetRequiredService<INodeIdSerializerAccessor>()
            .Serializer
            .Format("Review", 1);

        // act
        // The first event carries the raw database key (not a valid global id), so the node lookup
        // that fetches `body` fails. The second event carries the encoded global id and resolves.
        using var response = await client.PostAsync(request, new Uri(GatewayUrl), cts.Token);
        await WaitForConsumerAsync(nats.Url, stream, expectedCount: 1, cts.Token);
        await PublishAsync(nats.Url, Topic, """{"review":{"id":1}}""", cts.Token);
        await PublishAsync(nats.Url, Topic, "{\"review\":{\"id\":\"" + validId + "\"}}", cts.Token);

        var results = new List<OperationResult>();
        await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            results.Add(result);

            if (results.Count == 2)
            {
                break;
            }
        }

        // assert
        // First event: the message carried a raw int id, so the relay `node(id: ID!)` enrichment
        // lookup is fed an int where a string global id is required. That error is integrated and
        // `body` (non-null) null-propagates to the non-null root; the stream is NOT torn down.
        // Second event: the stream recovered and delivered the enriched review.
        Assert.Equal(2, results.Count);
        results.MatchInlineSnapshots(
            [
                """
                {
                  "data": null,
                  "errors": [
                    {
                      "message": "The argument literal representation is `HotChocolate.Language.IntValueNode` which is not compatible with the request literal type `HotChocolate.Language.StringValueNode`.",
                      "path": [
                        "onCreateReview",
                        "review",
                        "body"
                      ],
                      "extensions": {
                        "fieldName": "node",
                        "argumentName": "id",
                        "requestedType": "HotChocolate.Language.StringValueNode",
                        "actualType": "HotChocolate.Language.IntValueNode"
                      }
                    }
                  ]
                }
                """,
                """
                {
                  "data": {
                    "onCreateReview": {
                      "review": {
                        "id": "UmV2aWV3OjE=",
                        "body": "A great read"
                      },
                      "cursor": "Mg=="
                    }
                  }
                }
                """
            ]);
    }

    private static async Task CreateStreamAsync(string url, string stream, string subject, CancellationToken ct)
    {
        await using var connection = new NatsConnection(new NatsOpts { Url = url });
        var js = new NatsJSContext(connection);
        await js.CreateStreamAsync(new StreamConfig { Name = stream, Subjects = [subject] }, ct);
    }

    private static async Task PublishAsync(string url, string subject, string body, CancellationToken ct)
    {
        await using var connection = new NatsConnection(new NatsOpts { Url = url });
        var js = new NatsJSContext(connection);
        await js.PublishAsync(subject, Encoding.UTF8.GetBytes(body), cancellationToken: ct);
    }

    // A fresh JetStream subscription uses DeliverPolicy.New, so it only observes events published
    // after its ephemeral consumer exists on the server. Waiting until the stream reports the
    // expected consumer count makes "subscribe then publish" deterministic instead of racing a
    // fixed delay. The consumer name is server-generated, so we poll by count, not by name.
    private static async Task WaitForConsumerAsync(
        string url,
        string stream,
        int expectedCount,
        CancellationToken cancellationToken)
    {
        await using var connection = new NatsConnection(new NatsOpts { Url = url });
        var js = new NatsJSContext(connection);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var info = await js.GetStreamAsync(stream, cancellationToken: cancellationToken);

            if (info.Info.State.ConsumerCount >= expectedCount)
            {
                return;
            }

            await Task.Delay(25, cancellationToken);
        }
    }

    public static class ReviewsApi
    {
        public class Query
        {
            public string Version => "1.0.0";

            // Mirrors the demo's `[Lookup, NodeResolver] GetReviewByIdAsync(int id)`.
            [Lookup, NodeResolver]
            public Review? GetReviewById(int id)
                => id == 1 ? new Review(1, "A great read") : null;
        }

        public class Subscription
        {
            [EventStream("review { id }", Topic = Topic, Broker = BrokerName)]
            public ReviewCreated OnCreateReview([EventCursor] string? after)
                => EventStream.Create<ReviewCreated>(after);
        }

        public record ReviewCreated(Review Review, [property: EventCursor] string Cursor);

        public record Review(int Id, string Body);
    }

    private sealed class JetStreamNatsFixture : IAsyncDisposable
    {
        private readonly IContainer _container;

        private JetStreamNatsFixture(IContainer container) => _container = container;

        public string Url => $"nats://localhost:{_container.GetMappedPublicPort(4222)}";

        public static async Task<JetStreamNatsFixture> StartAsync()
        {
            var fixture = new JetStreamNatsFixture(
                new ContainerBuilder("nats:2.10-alpine")
                    .WithPortBinding(4222, assignRandomHostPort: true)
                    .WithCommand("-js")
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(4222))
                    .Build());

            await fixture._container.StartAsync();
            await fixture.WaitForConnectionAsync();
            return fixture;
        }

        private async Task WaitForConnectionAsync()
        {
            var attempt = 0;
            while (true)
            {
                try
                {
                    await using var connection = new NatsConnection(new NatsOpts { Url = Url });
                    await connection.ConnectAsync();
                    return;
                }
                catch (NatsException) when (++attempt < 20)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250));
                }
            }
        }

        public async ValueTask DisposeAsync() => await _container.DisposeAsync();
    }
}
