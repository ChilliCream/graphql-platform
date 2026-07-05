using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Subscriptions.NATS;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Mutable;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace HotChocolate.Fusion;

/// <summary>
/// A real end-to-end test for the resumable event-stream subscription feature. It composes an
/// HotChocolate source schema authored with the <c>[EventStream]</c>/<c>[EventCursor]</c> attributes
/// through Fusion, drives the resulting subscription through the gateway executor, and runs the
/// reception and resume path against a real NATS JetStream broker (provisioned via Testcontainers).
/// Unlike the in-memory broker double, the cursor here is a genuine JetStream stream sequence and the
/// resume replay is performed by NATS itself.
/// </summary>
public sealed class NatsEventStreamGatewayTests
{
    private const string BrokerName = "nats";

    // The single static topic the schema declares, with no "{$args.id}" templating.
    private const string Topic = "onUserCreated";

    [Fact]
    public async Task Subscribe_Should_ContinueAfterCursor_When_ResumedAgainstRealNatsJetStream()
    {
        // arrange
        await using var nats = await JetStreamNatsFixture.StartAsync();
        var stream = "S" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await CreateStreamAsync(nats.Url, stream, Topic, cts.Token);

        var executor = await BuildGatewayAsync(nats.Url, stream);

        // act
        // Drive the initial subscription, publish two events to the single NATS subject, and receive
        // both (each carrying a real JetStream sequence cursor). Then open a fresh subscription that
        // resumes from the first event's cursor and observe that NATS replays strictly after it.
        var initialEvents = await CollectEventsAsync(
            executor,
            """
            subscription {
              onUserCreated {
                user { id }
                cursor
              }
            }
            """,
            count: 2,
            async () =>
            {
                await WaitForConsumerAsync(nats.Url, stream, expectedCount: 1, cts.Token);
                await PublishAsync(nats.Url, Topic, """{"user":{"id":"u1"}}""", cts.Token);
                await PublishAsync(nats.Url, Topic, """{"user":{"id":"u2"}}""", cts.Token);
            },
            cts.Token);

        var firstCursor = ReadCursor(initialEvents[0]);

        var resumedEvents = await CollectEventsAsync(
            executor,
            $$"""
            subscription {
              onUserCreated(after: "{{firstCursor}}") {
                user { id }
                cursor
              }
            }
            """,
            count: 2,
            async () => await PublishAsync(nats.Url, Topic, """{"user":{"id":"u3"}}""", cts.Token),
            cts.Token);

        // assert
        // The initial stream delivers u1 then u2, each carrying a real JetStream sequence cursor
        // (base64 of the stream sequence: 1 -> "MQ==", 2 -> "Mg=="). The cursors are deterministic
        // because the stream is freshly created per test, so sequencing starts at 1. Resuming after
        // u1's cursor ("MQ==") must skip u1 and replay u2, then deliver u3 once published, proving the
        // stream continues strictly after the supplied resume cursor and never re-delivers u1.
        string.Join("\n---\n", initialEvents).MatchInlineSnapshot(
            """
            {
              "data": {
                "onUserCreated": {
                  "user": {
                    "id": "u1"
                  },
                  "cursor": "MQ=="
                }
              }
            }
            ---
            {
              "data": {
                "onUserCreated": {
                  "user": {
                    "id": "u2"
                  },
                  "cursor": "Mg=="
                }
              }
            }
            """);

        string.Join("\n---\n", resumedEvents).MatchInlineSnapshot(
            """
            {
              "data": {
                "onUserCreated": {
                  "user": {
                    "id": "u2"
                  },
                  "cursor": "Mg=="
                }
              }
            }
            ---
            {
              "data": {
                "onUserCreated": {
                  "user": {
                    "id": "u3"
                  },
                  "cursor": "Mw=="
                }
              }
            }
            """);
    }

    private static async Task<IRequestExecutor> BuildGatewayAsync(
        string natsUrl,
        string stream)
    {
        var sourceSchemaSdl = await PrintSourceSchemaSdlAsync(
            b => b.AddSubscriptionType<AttributeSubscriptions>());

        var schemaDocument = ComposeSchema(sourceSchemaSdl, addFusionDefinitions: true)
            .ToSyntaxNode();

        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddNatsEventStreamBroker(BrokerName, o =>
        {
            o.Url = natsUrl;
            o.JetStream = new NatsJetStreamOptions
            {
                Stream = stream
            };
        });

        services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(schemaDocument);

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<string> PrintSourceSchemaSdlAsync(
        Action<IRequestExecutorBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services
            .AddGraphQL("events")
            .AddQueryType<EventsQuery>()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddSourceSchemaDefaults();

        configure(builder);

        await using var provider = services.BuildServiceProvider();
        var schema = await provider.GetSchemaAsync(
            "events",
            TestContext.Current.CancellationToken);

        return schema.ToString();
    }

    private static MutableSchemaDefinition ComposeSchema(
        string sourceSchemaSdl,
        bool addFusionDefinitions)
    {
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [new SourceSchemaText("EVENTS", sourceSchemaSdl)],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = addFusionDefinitions } },
            log);

        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            var details = string.Join(
                Environment.NewLine,
                log.Select(e => e.Message));
            throw new InvalidOperationException(
                result.Errors[0].Message + Environment.NewLine + details);
        }

        return result.Value;
    }

    private static async Task<List<string>> CollectEventsAsync(
        IRequestExecutor executor,
        string document,
        int count,
        Func<Task> afterSubscribed,
        CancellationToken cancellationToken)
    {
        var request = OperationRequestBuilder.New()
            .SetDocument(document)
            .Build();

        var result = await executor.ExecuteAsync(request, cancellationToken);
        var stream = result.ExpectResponseStream();
        var events = new List<string>();

        var reader = ReadAsync();

        await afterSubscribed();

        await reader;
        await result.DisposeAsync();
        return events;

        async Task ReadAsync()
        {
            await foreach (var operationResult in stream
                .ReadResultsAsync()
                .WithCancellation(cancellationToken))
            {
                events.Add(operationResult.ToJson());

                if (events.Count == count)
                {
                    break;
                }
            }
        }
    }

    private static async Task CreateStreamAsync(
        string url,
        string stream,
        string subject,
        CancellationToken cancellationToken)
    {
        await using var connection = new NatsConnection(new NatsOpts { Url = url });
        var js = new NatsJSContext(connection);
        await js.CreateStreamAsync(
            new StreamConfig
            {
                Name = stream,
                Subjects = [subject]
            },
            cancellationToken);
    }

    private static async Task PublishAsync(
        string url,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        await using var connection = new NatsConnection(new NatsOpts { Url = url });
        var js = new NatsJSContext(connection);
        await js.PublishAsync(
            subject,
            System.Text.Encoding.UTF8.GetBytes(body),
            cancellationToken: cancellationToken);
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

    private static string ReadCursor(string operationResultJson)
    {
        using var document = System.Text.Json.JsonDocument.Parse(operationResultJson);
        return document.RootElement
            .GetProperty("data")
            .GetProperty("onUserCreated")
            .GetProperty("cursor")
            .GetString()!;
    }

    [GraphQLName("Query")]
    public class EventsQuery
    {
        public string Version => "1.0.0";
    }

    [GraphQLName("Subscription")]
    public class AttributeSubscriptions
    {
        [EventStream("user { id }", Topic = Topic, Broker = BrokerName)]
        public OnUserCreatedEvent OnUserCreated([EventCursor] string? after)
            => EventStream.Create<OnUserCreatedEvent>(after);
    }

    public record OnUserCreatedEvent(User User, [property: EventCursor] string Cursor);

    public record User(string Id);

    private sealed class JetStreamNatsFixture : IAsyncDisposable
    {
        private readonly IContainer _container;

        private JetStreamNatsFixture(IContainer container)
        {
            _container = container;
        }

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
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await _container.DisposeAsync();
        }
    }
}
