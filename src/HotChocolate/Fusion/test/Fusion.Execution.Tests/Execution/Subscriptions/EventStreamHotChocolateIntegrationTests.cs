using System.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Subscriptions;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Mutable;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

/// <summary>
/// End-to-end tests that start from an HotChocolate source schema authored with the
/// <c>[EventStream]</c>/<c>[EventCursor]</c> attributes (and a parallel schema authored with the
/// fluent API), compose it through Fusion, then execute and resume the resulting resumable
/// subscription against an in-memory broker.
/// </summary>
public sealed class EventStreamHotChocolateIntegrationTests
{
    private const string BrokerName = "memory";

    [Fact]
    public async Task Compose_Should_DeriveSubscribeDirective_When_SchemaIsAttributeAuthored()
    {
        // arrange
        var sdl = await PrintSourceSchemaSdlAsync(
            b => b.AddSubscriptionType<AttributeSubscriptions>());

        // act
        var composed = Compose(sdl);

        // assert
        // The @fusion__subscribe directive must carry the topics, broker, cursorField and
        // cursorArgument derived from the attribute-authored @subscribe/@eventCursor markers. A
        // directive or argument-name mismatch would silently drop @subscribe and this snapshot
        // would lose the directive entirely.
        composed.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Compose_Should_DeriveIdenticalSubscribeDirective_When_SchemaIsFluentAuthored()
    {
        // arrange
        var attributeSdl = await PrintSourceSchemaSdlAsync(
            b => b.AddSubscriptionType<AttributeSubscriptions>());
        var fluentSdl = await PrintSourceSchemaSdlAsync(
            b => b.AddSubscriptionType<FluentSubscriptionType>());

        // act
        var attributeComposed = Compose(attributeSdl);
        var fluentComposed = Compose(fluentSdl);

        // assert
        // The fluent composition is pinned to the committed ground-truth SDL so an identical
        // regression in both authoring surfaces (which the equality check alone would not catch)
        // still fails here. The equality check then proves both surfaces compose to the same
        // execution schema, including the derived @fusion__subscribe directive.
        fluentComposed.MatchSnapshot(extension: ".graphql");
        Assert.Equal(attributeComposed, fluentComposed);
    }

    [Fact]
    public async Task Subscribe_Should_ContinueAfterCursor_When_ResumedFromPreviousCursor()
    {
        // arrange
        var hub = new ResumableEventStreamBrokerHub();
        var services = CreateServices(hub);
        var sdl = await PrintSourceSchemaSdlAsync(
            b => b.AddSubscriptionType<AttributeSubscriptions>());
        var executor = await BuildGatewayAsync(services, sdl);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // The single static topic that "onUserCreated-{$args.id}" expands to for id "42".
        const string topic = "onUserCreated-42";

        // act
        // Drive the initial subscription, receive the first two events (each carrying a cursor),
        // then open a fresh subscription that resumes from the first event's cursor.
        var initialEvents = await CollectEventsAsync(
            executor,
            """
            subscription {
              onUserCreated(id: "42") {
                user { id }
                cursor
              }
            }
            """,
            count: 2,
            () => PublishAfterSubscribedAsync(
                hub,
                topic,
                [
                    ("""{"user":{"id":"u1"}}""", "Y3Vyc29yLTE="),
                    ("""{"user":{"id":"u2"}}""", "Y3Vyc29yLTI="),
                    ("""{"user":{"id":"u3"}}""", "Y3Vyc29yLTM=")
                ],
                cts.Token),
            cts.Token);

        var resumedEvents = await CollectEventsAsync(
            executor,
            """
            subscription {
              onUserCreated(id: "42", after: "Y3Vyc29yLTE=") {
                user { id }
                cursor
              }
            }
            """,
            count: 2,
            afterSubscribed: null,
            cts.Token);

        // assert
        // The executor must forward the client's resume cursor verbatim to the broker so the
        // broker can position the resumed stream. The initial stream delivers events 1 (u1) and 2
        // (u2). Resuming after event 1's cursor (Y3Vyc29yLTE=) must replay events 2 (u2) and 3
        // (u3), proving the stream continues strictly after the supplied resume cursor and never
        // re-delivers the cursor's own event.
        Assert.Equal("Y3Vyc29yLTE=", hub.GetLastSubscribedCursor(topic));

        Assert.Collection(
            initialEvents,
            first => Assert.Contains("\"id\": \"u1\"", first),
            second => Assert.Contains("\"id\": \"u2\"", second));

        string.Join("\n---\n", resumedEvents).MatchInlineSnapshot(
            """
            {
              "data": {
                "onUserCreated": {
                  "user": {
                    "id": "u2"
                  },
                  "cursor": "Y3Vyc29yLTI="
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
                  "cursor": "Y3Vyc29yLTM="
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Subscribe_Should_ExpandTopicTemplate_When_ArgumentIsProvided()
    {
        // arrange
        var hub = new ResumableEventStreamBrokerHub();
        var services = CreateServices(hub);
        var sdl = await PrintSourceSchemaSdlAsync(
            b => b.AddSubscriptionType<AttributeSubscriptions>());
        var executor = await BuildGatewayAsync(services, sdl);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // The topic the literal template would yield if "{$args.id}" was never substituted.
        const string literalTemplateTopic = "onUserCreated-{$args.id}";
        const string expandedTopic = "onUserCreated-7";

        // act
        // The topic template "onUserCreated-{$args.id}" must expand from the subscription
        // argument at subscribe time. Once the subscription is established, the broker hub must
        // hold a subscriber on the expanded topic and none on the unexpanded literal template;
        // only a publish to the expanded topic then reaches the client.
        var events = await CollectEventsAsync(
            executor,
            """
            subscription {
              onUserCreated(id: "7") {
                user { id }
                cursor
              }
            }
            """,
            count: 1,
            async () =>
            {
                // Wait only for the executor to subscribe to some topic, then assert directly which
                // topic it landed on. A wrong expansion fails here immediately on the subscriber
                // counts rather than by waiting out a wall-clock timeout.
                await WaitForAnySubscriberAsync(hub, cts.Token);

                Assert.Equal(1, hub.GetSubscriberCount(expandedTopic));
                Assert.Equal(0, hub.GetSubscriberCount(literalTemplateTopic));

                await hub.PublishAsync(
                    expandedTopic,
                    CreateMessage("""{"user":{"id":"u7"}}""", "Y3Vyc29yLTc="),
                    cts.Token);
            },
            cts.Token);

        // assert
        events.Single().MatchInlineSnapshot(
            """
            {
              "data": {
                "onUserCreated": {
                  "user": {
                    "id": "u7"
                  },
                  "cursor": "Y3Vyc29yLTc="
                }
              }
            }
            """);
    }

    private static ServiceCollection CreateServices(ResumableEventStreamBrokerHub hub)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton(hub);
        services.AddSingleton<IInMemoryEventStreamPublisher>(hub);
        services.AddSingleton<IEventStreamBrokerFactory, DefaultEventStreamBrokerFactory>();
        services.AddKeyedSingleton<IEventStreamBrokerProvider>(
            BrokerName,
            (_, _) => new ResumableEventStreamBrokerProvider(hub));

        return services;
    }

    private static async Task<IRequestExecutor> BuildGatewayAsync(
        ServiceCollection services,
        string sourceSchemaSdl)
    {
        // The executor needs the full fusion definitions in the execution schema, so compose with
        // the default fusion definitions and feed the resulting document into the gateway.
        var schemaDocument = ComposeSchema(sourceSchemaSdl, addFusionDefinitions: true)
            .ToSyntaxNode();

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

    private static string Compose(string sourceSchemaSdl)
        => ComposeSchema(sourceSchemaSdl, addFusionDefinitions: false).ToString();

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
        Func<Task>? afterSubscribed,
        CancellationToken cancellationToken)
    {
        var request = OperationRequestBuilder.New()
            .SetDocument(document)
            .Build();

        var result = await executor.ExecuteAsync(request, cancellationToken);
        var stream = result.ExpectResponseStream();
        var events = new List<string>();

        var reader = ReadAsync();

        if (afterSubscribed is not null)
        {
            await afterSubscribed();
        }

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

    private static async Task PublishAfterSubscribedAsync(
        ResumableEventStreamBrokerHub hub,
        string topic,
        (string Body, string Cursor)[] messages,
        CancellationToken cancellationToken)
    {
        await WaitForSubscribersAsync(hub, topic, count: 1, cancellationToken);

        foreach (var (body, cursor) in messages)
        {
            await hub.PublishAsync(
                topic,
                CreateMessage(body, cursor),
                cancellationToken);
        }
    }

    private static async Task WaitForSubscribersAsync(
        ResumableEventStreamBrokerHub hub,
        string topic,
        int count,
        CancellationToken cancellationToken)
    {
        while (hub.GetSubscriberCount(topic) < count)
        {
            await Task.Delay(10, cancellationToken);
        }
    }

    private static async Task WaitForAnySubscriberAsync(
        ResumableEventStreamBrokerHub hub,
        CancellationToken cancellationToken)
    {
        while (hub.GetTotalSubscriberCount() < 1)
        {
            await Task.Delay(10, cancellationToken);
        }
    }

    private static EventMessage CreateMessage(string body, string cursor)
    {
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
        var cursorBytes = System.Text.Encoding.UTF8.GetBytes(cursor);
        var owner = MemoryPool<byte>.Shared.Rent(bodyBytes.Length + cursorBytes.Length);

        bodyBytes.CopyTo(owner.Memory.Span);
        cursorBytes.CopyTo(owner.Memory.Span[bodyBytes.Length..]);

        return new EventMessage(
            owner,
            0..bodyBytes.Length,
            bodyBytes.Length..(bodyBytes.Length + cursorBytes.Length));
    }

    [GraphQLName("Query")]
    public class EventsQuery
    {
        public string Version => "1.0.0";
    }

    // attribute authoring of the @subscribe and @eventCursor directives
    [GraphQLName("Subscription")]
    public class AttributeSubscriptions
    {
        [EventStream("user { id }", Topic = "onUserCreated-{$args.id}", Broker = BrokerName)]
        public OnUserCreatedEvent OnUserCreated([EventCursor] string? after, string id)
            => EventStream.Create<OnUserCreatedEvent>(after);
    }

    // fluent authoring of the same @subscribe and @eventCursor directives, named identically so
    // the two authoring surfaces compose into the same execution schema.
    public class FluentSubscriptionType : ObjectType<FluentSubscriptions>
    {
        protected override void Configure(IObjectTypeDescriptor<FluentSubscriptions> descriptor)
        {
            descriptor.Name("Subscription");

            descriptor
                .Field(f => f.OnUserCreated(default, default!))
                .EventStream("user { id }", "onUserCreated-{$args.id}", BrokerName)
                .Argument("after", a => a.EventCursor());
        }
    }

    public class FluentSubscriptions
    {
        public OnUserCreatedEvent OnUserCreated(string? after, string id)
            => EventStream.Create<OnUserCreatedEvent>(after);
    }

    public record OnUserCreatedEvent(User User, [property: EventCursor] string Cursor);

    public record User(string Id);
}
