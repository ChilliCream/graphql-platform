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
    public async Task Compose_Should_DeriveEventStreamDirective_When_SchemaIsAttributeAuthored()
    {
        // arrange
        var sdl = await PrintSourceSchemaSdlAsync(
            b => b.AddSubscriptionType<AttributeSubscriptions>());

        // act
        var composed = Compose(sdl);

        // assert
        // The @fusion__eventStream directive must carry the topics, broker, cursorField and
        // cursorArgument derived from the attribute-authored @eventStream/@eventCursor markers. A
        // directive or argument-name mismatch would silently drop @eventStream and this snapshot
        // would lose the directive entirely.
        composed.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Compose_Should_DeriveIdenticalEventStreamDirective_When_SchemaIsFluentAuthored()
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
        // execution schema, including the derived @fusion__eventStream directive.
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

        string.Join("\n---\n", initialEvents).MatchInlineSnapshot(
            """
            {
              "data": {
                "onUserCreated": {
                  "user": {
                    "id": "u1"
                  },
                  "cursor": "Y3Vyc29yLTE="
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
                  "cursor": "Y3Vyc29yLTI="
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
    public async Task Subscribe_Should_ResolveTypename_When_ConcretePayloadSelectsTypename()
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
        // The broker body carries no __typename at either the root payload or the nested user
        // object, so both __typename values must be synthesized from the schema along the
        // event-stream path, which has no subgraph round-trip to echo them back.
        var events = await CollectEventsAsync(
            executor,
            """
            subscription {
              onUserCreated(id: "42") {
                __typename
                user { __typename id }
                cursor
              }
            }
            """,
            count: 1,
            () => PublishAfterSubscribedAsync(
                hub,
                topic,
                [("""{"user":{"id":"u1"}}""", "Y3Vyc29yLTE=")],
                cts.Token),
            cts.Token);

        // assert
        // __typename must resolve to the concrete schema type at every nesting level the client
        // selected it, even though the raw broker body contains neither value.
        events.Single().MatchInlineSnapshot(
            """
            {
              "data": {
                "onUserCreated": {
                  "__typename": "OnUserCreatedEvent",
                  "user": {
                    "__typename": "User",
                    "id": "u1"
                  },
                  "cursor": "Y3Vyc29yLTE="
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

    [Fact]
    public async Task Subscribe_Should_ResolveAbstractTypename_When_PayloadIsInterface()
    {
        // arrange
        var hub = new ResumableEventStreamBrokerHub();
        var services = CreateServices(hub);
        var sdl = await PrintSourceSchemaSdlAsync(
            b => b
                .AddSubscriptionType<AnimalSubscriptions>()
                .AddType<Cat>()
                .AddType<Dog>());
        var executor = await BuildGatewayAsync(services, sdl);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        const string topic = "onAnimalCreated";

        // act
        // An interface payload carries its concrete type in the body's __typename. The two events
        // resolve to different runtime types, so each must select the matching inline-fragment
        // fields and report the __typename carried by its own body.
        var events = await CollectEventsAsync(
            executor,
            """
            subscription {
              onAnimalCreated {
                __typename
                ... on Cat { name livesLeft }
                ... on Dog { name goodBoy }
              }
            }
            """,
            count: 2,
            () => PublishAfterSubscribedAsync(
                hub,
                topic,
                [
                    ("""{"__typename":"Cat","name":"Whiskers","livesLeft":9}""", "Y3Vyc29yLTE="),
                    ("""{"__typename":"Dog","name":"Rex","goodBoy":true}""", "Y3Vyc29yLTI=")
                ],
                cts.Token),
            cts.Token);

        // assert
        // The body's __typename selects the runtime type, which gates the inline fragments: the Cat
        // event resolves only the Cat fields and the Dog event only the Dog fields, each reporting
        // its own __typename rather than a synthesized one.
        string.Join("\n---\n", events).MatchInlineSnapshot(
            """
            {
              "data": {
                "onAnimalCreated": {
                  "__typename": "Cat",
                  "name": "Whiskers",
                  "livesLeft": 9
                }
              }
            }
            ---
            {
              "data": {
                "onAnimalCreated": {
                  "__typename": "Dog",
                  "name": "Rex",
                  "goodBoy": true
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Subscribe_Should_ResolveTypenameForEachElement_When_PayloadContainsObjectList()
    {
        // arrange
        var hub = new ResumableEventStreamBrokerHub();
        var services = CreateServices(hub);
        var sdl = await PrintSourceSchemaSdlAsync(
            b => b.AddSubscriptionType<ListSubscriptions>());
        var executor = await BuildGatewayAsync(services, sdl);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        const string topic = "onUsersCreated";

        // act
        // The body carries no __typename on the payload or on any list element, so the concrete
        // type name must be synthesized for the root payload and injected into every element of the
        // list.
        var events = await CollectEventsAsync(
            executor,
            """
            subscription {
              onUsersCreated {
                __typename
                users { __typename id }
              }
            }
            """,
            count: 1,
            () => PublishAfterSubscribedAsync(
                hub,
                topic,
                [("""{"users":[{"id":"u1"},{"id":"u2"}]}""", "Y3Vyc29yLTE=")],
                cts.Token),
            cts.Token);

        // assert
        // Every list element resolves the concrete element type name, not just the first, and the
        // element fields remain intact.
        events.Single().MatchInlineSnapshot(
            """
            {
              "data": {
                "onUsersCreated": {
                  "__typename": "OnUsersCreatedEvent",
                  "users": [
                    {
                      "__typename": "User",
                      "id": "u1"
                    },
                    {
                      "__typename": "User",
                      "id": "u2"
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Subscribe_Should_ResolveNestedAbstractTypename_When_FieldIsInterface()
    {
        // arrange
        var hub = new ResumableEventStreamBrokerHub();
        var services = CreateServices(hub);
        var sdl = await PrintSourceSchemaSdlAsync(
            b => b
                .AddSubscriptionType<ContentSubscriptions>()
                .AddType<Article>()
                .AddType<Photo>());
        var executor = await BuildGatewayAsync(services, sdl);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        const string topic = "onContentCreated";

        // act
        // The root payload is a concrete object whose `content` field is an interface. The root
        // __typename is synthesized from the schema, while each nested content object carries its
        // concrete type in the body's __typename, which gates the inline fragments at the nested level.
        var events = await CollectEventsAsync(
            executor,
            """
            subscription {
              onContentCreated {
                __typename
                content {
                  __typename
                  id
                  ... on Article { headline }
                  ... on Photo { url }
                }
              }
            }
            """,
            count: 2,
            () => PublishAfterSubscribedAsync(
                hub,
                topic,
                [
                    ("""{"content":{"__typename":"Article","id":"a1","headline":"Breaking"}}""", "Y3Vyc29yLTE="),
                    ("""{"content":{"__typename":"Photo","id":"p1","url":"https://img/1.png"}}""", "Y3Vyc29yLTI=")
                ],
                cts.Token),
            cts.Token);

        // assert
        // Each nested content resolves only its matching fragment fields and reports the body's
        // __typename, while the concrete root payload reports a synthesized OnContentCreatedEvent.
        events.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "onContentCreated": {
                  "__typename": "OnContentCreatedEvent",
                  "content": {
                    "__typename": "Article",
                    "id": "a1",
                    "headline": "Breaking"
                  }
                }
              }
            }
            """,
            """
            {
              "data": {
                "onContentCreated": {
                  "__typename": "OnContentCreatedEvent",
                  "content": {
                    "__typename": "Photo",
                    "id": "p1",
                    "url": "https://img/1.png"
                  }
                }
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task Subscribe_Should_ResolveUnionTypename_When_PayloadIsUnion()
    {
        // arrange
        var hub = new ResumableEventStreamBrokerHub();
        var services = CreateServices(hub);
        var sdl = await PrintSourceSchemaSdlAsync(
            b => b
                .AddSubscriptionType<SearchSubscriptions>()
                .AddType<Product>()
                .AddType<Category>());
        var executor = await BuildGatewayAsync(services, sdl);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        const string topic = "onSearchHit";

        // act
        // A union payload is fragment-only and carries its concrete member in the body's __typename.
        // The two events resolve to different members, each selecting only its own fragment fields.
        var events = await CollectEventsAsync(
            executor,
            """
            subscription {
              onSearchHit {
                __typename
                ... on Product { name }
                ... on Category { title }
              }
            }
            """,
            count: 2,
            () => PublishAfterSubscribedAsync(
                hub,
                topic,
                [
                    ("""{"__typename":"Product","name":"Widget"}""", "Y3Vyc29yLTE="),
                    ("""{"__typename":"Category","title":"Tools"}""", "Y3Vyc29yLTI=")
                ],
                cts.Token),
            cts.Token);

        // assert
        // The body's __typename selects the runtime member, gating the inline fragments: the Product
        // event resolves only the Product field and the Category event only the Category field.
        events.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "onSearchHit": {
                  "__typename": "Product",
                  "name": "Widget"
                }
              }
            }
            """,
            """
            {
              "data": {
                "onSearchHit": {
                  "__typename": "Category",
                  "title": "Tools"
                }
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task Subscribe_Should_ResolveAbstractTypenameForEachElement_When_PayloadContainsInterfaceList()
    {
        // arrange
        var hub = new ResumableEventStreamBrokerHub();
        var services = CreateServices(hub);
        var sdl = await PrintSourceSchemaSdlAsync(
            b => b
                .AddSubscriptionType<AnimalListSubscriptions>()
                .AddType<Cat>()
                .AddType<Dog>());
        var executor = await BuildGatewayAsync(services, sdl);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        const string topic = "onAnimalsCreated";

        // act
        // Every list element is an interface whose concrete type is carried by that element's body
        // __typename, so each element resolves its own runtime type and fragment fields independently.
        var events = await CollectEventsAsync(
            executor,
            """
            subscription {
              onAnimalsCreated {
                __typename
                animals {
                  __typename
                  ... on Cat { name livesLeft }
                  ... on Dog { name goodBoy }
                }
              }
            }
            """,
            count: 1,
            () => PublishAfterSubscribedAsync(
                hub,
                topic,
                [
                    (
                        """{"animals":[{"__typename":"Cat","name":"Whiskers","livesLeft":9},{"__typename":"Dog","name":"Rex","goodBoy":true}]}""",
                        "Y3Vyc29yLTE="
                    )
                ],
                cts.Token),
            cts.Token);

        // assert
        // Each element resolves the concrete type from its own body __typename, while the concrete
        // root payload reports a synthesized OnAnimalsCreatedEvent.
        events.Single().MatchInlineSnapshot(
            """
            {
              "data": {
                "onAnimalsCreated": {
                  "__typename": "OnAnimalsCreatedEvent",
                  "animals": [
                    {
                      "__typename": "Cat",
                      "name": "Whiskers",
                      "livesLeft": 9
                    },
                    {
                      "__typename": "Dog",
                      "name": "Rex",
                      "goodBoy": true
                    }
                  ]
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

    // attribute authoring of the @eventStream and @eventCursor directives
    [GraphQLName("Subscription")]
    public class AttributeSubscriptions
    {
        [EventStream("user { id }", Topic = "onUserCreated-{$args.id}", Broker = BrokerName)]
        public OnUserCreatedEvent OnUserCreated([EventCursor] string? after, string id)
            => EventStream.Create<OnUserCreatedEvent>(after);
    }

    // fluent authoring of the same @eventStream and @eventCursor directives, named identically so
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

    // attribute authoring of an event stream whose payload carries a list of a concrete object
    // type, so __typename must be synthesized for every element of the list.
    [GraphQLName("Subscription")]
    public class ListSubscriptions
    {
        [EventStream("users { id }", Topic = "onUsersCreated", Broker = BrokerName)]
        public OnUsersCreatedEvent OnUsersCreated()
            => EventStream.Create<OnUsersCreatedEvent>();
    }

    public record OnUsersCreatedEvent(IReadOnlyList<User> Users);

    // attribute authoring of an event stream whose payload is an interface, so the concrete type is
    // carried by the broker body's __typename rather than synthesized.
    [GraphQLName("Subscription")]
    public class AnimalSubscriptions
    {
        [EventStream(
            "__typename name ... on Cat { livesLeft } ... on Dog { goodBoy }",
            Topic = "onAnimalCreated",
            Broker = BrokerName)]
        public IAnimal OnAnimalCreated()
            => EventStream.Create<IAnimal>();
    }

    [InterfaceType("Animal")]
    public interface IAnimal
    {
        string Name { get; }
    }

    public record Cat(string Name, int LivesLeft) : IAnimal;

    public record Dog(string Name, bool GoodBoy) : IAnimal;

    // attribute authoring of an event stream whose concrete payload contains a list of an
    // interface, so each element carries its concrete type in the body's __typename.
    [GraphQLName("Subscription")]
    public class AnimalListSubscriptions
    {
        [EventStream(
            "animals { __typename name ... on Cat { livesLeft } ... on Dog { goodBoy } }",
            Topic = "onAnimalsCreated",
            Broker = BrokerName)]
        public OnAnimalsCreatedEvent OnAnimalsCreated()
            => EventStream.Create<OnAnimalsCreatedEvent>();
    }

    public record OnAnimalsCreatedEvent(IReadOnlyList<IAnimal> Animals);

    // attribute authoring of an event stream whose concrete payload contains an interface field, so
    // the nested concrete type is carried by the body's __typename while the root is synthesized.
    [GraphQLName("Subscription")]
    public class ContentSubscriptions
    {
        [EventStream(
            "content { __typename id ... on Article { headline } ... on Photo { url } }",
            Topic = "onContentCreated",
            Broker = BrokerName)]
        public OnContentCreatedEvent OnContentCreated()
            => EventStream.Create<OnContentCreatedEvent>();
    }

    public record OnContentCreatedEvent(IContent Content);

    [InterfaceType("Content")]
    public interface IContent
    {
        string Id { get; }
    }

    public record Article(string Id, string Headline) : IContent;

    public record Photo(string Id, string Url) : IContent;

    // attribute authoring of an event stream whose payload is a union, which is fragment-only and
    // carries its concrete member type in the broker body's __typename.
    [GraphQLName("Subscription")]
    public class SearchSubscriptions
    {
        [EventStream(
            "__typename ... on Product { name } ... on Category { title }",
            Topic = "onSearchHit",
            Broker = BrokerName)]
        public ISearchResult OnSearchHit()
            => EventStream.Create<ISearchResult>();
    }

    [UnionType("SearchResult")]
    public interface ISearchResult;

    public record Product(string Name) : ISearchResult;

    public record Category(string Title) : ISearchResult;
}
