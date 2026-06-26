using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using HotChocolate.Fusion.Subscriptions.NATS;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace HotChocolate.Fusion;

/// <summary>
/// Real end-to-end tests that drive a Federated Event Stream subscription over the HTTP
/// GraphQL-SSE transport. The gateway is hosted in an ASP.NET Core test server with
/// <c>MapGraphQL</c>, the subscription is consumed with the GraphQL-over-HTTP client, and the
/// events are published to a real NATS JetStream broker (provisioned via Testcontainers). Unlike
/// the executor-level event-stream tests, these tests prove the reception, cross-schema entity
/// resolution, and operation-plan extension behavior on the wire.
/// </summary>
public class NatsEventStreamOverHttpTests : FusionTestBase
{
    private const string BrokerName = "nats";

    // The single static topic the schema declares, with no "{$args.id}" templating.
    private const string Topic = "onUserCreated";

    private const string GatewayUrl = "http://localhost:5000/graphql";

    [Fact]
    public async Task Subscribe_Should_DeliverEvent_When_PublishedToNatsOverHttp()
    {
        // arrange
        await using var nats = await JetStreamNatsFixture.StartAsync();
        var stream = "S" + Guid.NewGuid().ToString("N");
        var durable = "D" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await CreateStreamAsync(nats.Url, stream, Topic, cts.Token);

        using var events = CreateSourceSchema(
            "EVENTS",
            b => b
                .AddQueryType<Events.Query>()
                .AddSubscriptionType<Events.Subscription>()
                .ModifyOptions(o => o.StrictValidation = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("EVENTS", events)],
            configureServices: services => services.AddNatsEventStreamBroker(
                BrokerName,
                o =>
                {
                    o.Url = nats.Url;
                    o.JetStream = new NatsJetStreamOptions { Stream = stream, DurableConsumer = durable };
                }),
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            subscription {
              onUserCreated {
                user { id }
                cursor
              }
            }
            """);

        // act
        // Open the SSE response first so the gateway has subscribed to NATS, then publish a single
        // event. The first stream sequence is 1, whose base64 cursor is deterministically "MQ==".
        using var response = await client.PostAsync(request, new Uri(GatewayUrl), cts.Token);
        await PublishAsync(nats.Url, Topic, """{"user":{"id":"u1"}}""", cts.Token);

        // assert
        var received = false;
        await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            received = true;
            result.MatchInlineSnapshot(
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
                """);
            break;
        }

        Assert.True(received, "Expected at least one SSE event over the wire.");
    }

    [Fact]
    public async Task Subscribe_Should_ResolveConcreteTypename_When_QueriedOverHttp()
    {
        // arrange
        await using var nats = await JetStreamNatsFixture.StartAsync();
        var stream = "S" + Guid.NewGuid().ToString("N");
        var durable = "D" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await CreateStreamAsync(nats.Url, stream, Topic, cts.Token);

        using var events = CreateSourceSchema(
            "EVENTS",
            b => b
                .AddQueryType<Events.Query>()
                .AddSubscriptionType<Events.Subscription>()
                .ModifyOptions(o => o.StrictValidation = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("EVENTS", events)],
            configureServices: services => services.AddNatsEventStreamBroker(
                BrokerName,
                o =>
                {
                    o.Url = nats.Url;
                    o.JetStream = new NatsJetStreamOptions { Stream = stream, DurableConsumer = durable };
                }),
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            subscription {
              onUserCreated {
                __typename
                user {
                  __typename
                  id
                }
                cursor
              }
            }
            """);

        // act
        // The published payload carries no __typename, so the concrete type names below are
        // synthesized by the gateway at every nesting level rather than echoed from the broker.
        using var response = await client.PostAsync(request, new Uri(GatewayUrl), cts.Token);
        await PublishAsync(nats.Url, Topic, """{"user":{"id":"u1"}}""", cts.Token);

        // assert
        var received = false;
        await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            received = true;
            result.MatchInlineSnapshot(
                """
                {
                  "data": {
                    "onUserCreated": {
                      "__typename": "UserCreated",
                      "user": {
                        "__typename": "User",
                        "id": "u1"
                      },
                      "cursor": "MQ=="
                    }
                  }
                }
                """);
            break;
        }

        Assert.True(received, "Expected at least one SSE event over the wire.");
    }

    [Fact]
    public async Task Subscribe_Should_ResolveCrossSchemaEntityField_When_QueriedOverHttp()
    {
        // arrange
        await using var nats = await JetStreamNatsFixture.StartAsync();
        var stream = "S" + Guid.NewGuid().ToString("N");
        var durable = "D" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await CreateStreamAsync(nats.Url, stream, Topic, cts.Token);

        using var eventsServer = CreateSourceSchema(
            "EVENTS",
            b => b
                .AddQueryType<FederatedEvents.Query>()
                .AddSubscriptionType<FederatedEvents.Subscription>()
                .ModifyOptions(o => o.StrictValidation = false));

        using var accountsServer = CreateSourceSchema(
            "ACCOUNTS",
            b => b.AddQueryType<Accounts.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("EVENTS", eventsServer),
                ("ACCOUNTS", accountsServer)
            ],
            configureServices: services => services.AddNatsEventStreamBroker(
                BrokerName,
                o =>
                {
                    o.Url = nats.Url;
                    o.JetStream = new NatsJetStreamOptions { Stream = stream, DurableConsumer = durable };
                }),
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            subscription {
              onUserCreated {
                user {
                  id
                  name
                }
                cursor
              }
            }
            """);

        // act
        // EVENTS emits only the entity key (id); the gateway fetches name from ACCOUNTS by lookup
        // for each event, so name must resolve non-null over the wire.
        using var response = await client.PostAsync(request, new Uri(GatewayUrl), cts.Token);
        await PublishAsync(nats.Url, Topic, """{"user":{"id":"u1"}}""", cts.Token);

        // assert
        var received = false;
        await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            received = true;
            result.MatchInlineSnapshot(
                """
                {
                  "data": {
                    "onUserCreated": {
                      "user": {
                        "id": "u1",
                        "name": "User u1"
                      },
                      "cursor": "MQ=="
                    }
                  }
                }
                """);
            break;
        }

        Assert.True(received, "Expected at least one SSE event over the wire.");
    }

    [Fact]
    public async Task Subscribe_Should_ResolveNestedAbstractEntityTypename_When_PerTypeLookupOverHttp()
    {
        // arrange
        await using var nats = await JetStreamNatsFixture.StartAsync();
        var stream = "S" + Guid.NewGuid().ToString("N");
        var durable = "D" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await CreateStreamAsync(nats.Url, stream, Topic, cts.Token);

        using var eventsServer = CreateSourceSchema(
            "EVENTS",
            b => b
                .AddQueryType<NestedAbstractEntityEvents.Query>()
                .AddSubscriptionType<NestedAbstractEntityEvents.Subscription>()
                .AddType<NestedAbstractEntityEvents.Article>()
                .AddType<NestedAbstractEntityEvents.Video>()
                .ModifyOptions(o => o.StrictValidation = false));

        using var contentServer = CreateSourceSchema(
            "CONTENT",
            b => b
                .AddQueryType<Content.Query>()
                .AddType<Content.Article>()
                .AddType<Content.Video>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("EVENTS", eventsServer),
                ("CONTENT", contentServer)
            ],
            configureServices: services => services.AddNatsEventStreamBroker(
                BrokerName,
                o =>
                {
                    o.Url = nats.Url;
                    o.JetStream = new NatsJetStreamOptions { Stream = stream, DurableConsumer = durable };
                }),
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            subscription {
              onContentCreated {
                item {
                  __typename
                  ... on Article { id headline }
                  ... on Video { id caption }
                }
                cursor
              }
            }
            """);

        // act
        // The concrete payload's `item` field is abstract and carries only __typename and the entity
        // key in the body. Each event resolves to a different concrete entity (Article then Video),
        // and the gateway must fire the per-concrete-type lookup against CONTENT to fetch the
        // type-owned field (headline/caption) at the nested level.
        using var response = await client.PostAsync(request, new Uri(GatewayUrl), cts.Token);
        await PublishAsync(nats.Url, Topic, """{"item":{"__typename":"Article","id":"a1"}}""", cts.Token);
        await PublishAsync(nats.Url, Topic, """{"item":{"__typename":"Video","id":"v1"}}""", cts.Token);

        // assert
        // Each event reports the concrete entity __typename at the nested level, selects only its
        // matching fragment, and carries the cross-schema field fetched by that type's lookup
        // (headline for Article, caption for Video).
        var index = 0;
        await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            if (index == 0)
            {
                result.MatchInlineSnapshot(
                    """
                    {
                      "data": {
                        "onContentCreated": {
                          "item": {
                            "__typename": "Article",
                            "id": "a1",
                            "headline": "Article a1"
                          },
                          "cursor": "MQ=="
                        }
                      }
                    }
                    """);
            }
            else
            {
                result.MatchInlineSnapshot(
                    """
                    {
                      "data": {
                        "onContentCreated": {
                          "item": {
                            "__typename": "Video",
                            "id": "v1",
                            "caption": "Video v1"
                          },
                          "cursor": "Mg=="
                        }
                      }
                    }
                    """);
            }

            if (++index == 2)
            {
                break;
            }
        }

        Assert.Equal(2, index);
    }

    [Fact]
    public async Task Subscribe_Should_ResolveRootAbstractEntityTypename_When_PerTypeLookupOverHttp()
    {
        // arrange
        await using var nats = await JetStreamNatsFixture.StartAsync();
        var stream = "S" + Guid.NewGuid().ToString("N");
        var durable = "D" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await CreateStreamAsync(nats.Url, stream, Topic, cts.Token);

        using var eventsServer = CreateSourceSchema(
            "EVENTS",
            b => b
                .AddQueryType<RootAbstractEntityEvents.Query>()
                .AddSubscriptionType<RootAbstractEntityEvents.Subscription>()
                .AddType<AbstractEntityEvents.Author>()
                .AddType<AbstractEntityEvents.Book>()
                .ModifyOptions(o => o.StrictValidation = false));

        using var detailsServer = CreateSourceSchema(
            "DETAILS",
            b => b
                .AddQueryType<Details.Query>()
                .AddType<Details.Author>()
                .AddType<Details.Book>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("EVENTS", eventsServer),
                ("DETAILS", detailsServer)
            ],
            configureServices: services => services.AddNatsEventStreamBroker(
                BrokerName,
                o =>
                {
                    o.Url = nats.Url;
                    o.JetStream = new NatsJetStreamOptions { Stream = stream, DurableConsumer = durable };
                }),
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            subscription {
              onNodeCreated {
                __typename
                ... on Author { id name }
                ... on Book { id title }
              }
            }
            """);

        // act
        // The abstract payload is at the ROOT; the body carries only __typename and the entity key.
        // Each event resolves to a different concrete entity (Author then Book), and the gateway must
        // fire that type's lookup against DETAILS to fetch the type-owned field (name/title).
        using var response = await client.PostAsync(request, new Uri(GatewayUrl), cts.Token);
        await PublishAsync(nats.Url, Topic, """{"__typename":"Author","id":"a1"}""", cts.Token);
        await PublishAsync(nats.Url, Topic, """{"__typename":"Book","id":"b1"}""", cts.Token);

        // assert
        // Each event reports the concrete entity __typename, selects only its matching fragment, and
        // carries the cross-schema field fetched by that type's lookup (name for Author, title for Book).
        var results = new List<OperationResult>();
        await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            results.Add(result);

            if (results.Count == 2)
            {
                break;
            }
        }

        results.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "onNodeCreated": {
                      "__typename": "Author",
                      "id": "a1",
                      "name": "Author a1"
                    }
                  }
                }
                """,
                """
                {
                  "data": {
                    "onNodeCreated": {
                      "__typename": "Book",
                      "id": "b1",
                      "title": "Book b1"
                    }
                  }
                }
                """
            ]);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Subscribe_Should_IncludeOperationPlanExtension_When_OptedInOverHttp()
    {
        // arrange
        await using var nats = await JetStreamNatsFixture.StartAsync();
        var stream = "S" + Guid.NewGuid().ToString("N");
        var durable = "D" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await CreateStreamAsync(nats.Url, stream, Topic, cts.Token);

        using var events = CreateSourceSchema(
            "EVENTS",
            b => b
                .AddQueryType<Events.Query>()
                .AddSubscriptionType<Events.Subscription>()
                .ModifyOptions(o => o.StrictValidation = false));

        // AllowOperationPlanRequests defaults to true in the test harness, so the operation plan is
        // opt-in via the Fusion-Operation-Plan request header.
        using var gateway = await CreateCompositeSchemaAsync(
            [("EVENTS", events)],
            configureServices: services => services.AddNatsEventStreamBroker(
                BrokerName,
                o =>
                {
                    o.Url = nats.Url;
                    o.JetStream = new NatsJetStreamOptions { Stream = stream, DurableConsumer = durable };
                }));

        var httpClient = gateway.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Fusion-Operation-Plan", "1");
        using var client = GraphQLHttpClient.Create(httpClient);

        var request = new OperationRequest(
            """
            subscription {
              onUserCreated {
                user { id }
                cursor
              }
            }
            """);

        // act
        using var response = await client.PostAsync(request, new Uri(GatewayUrl), cts.Token);
        await PublishAsync(nats.Url, Topic, """{"user":{"id":"u1"}}""", cts.Token);

        // assert
        var received = false;
        await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            received = true;
            Assert.Equal(
                "u1",
                result.Data.GetProperty("onUserCreated").GetProperty("user").GetProperty("id").GetString());
            Assert.True(
                result.Extensions.TryGetProperty("fusion", out var fusion)
                && fusion.TryGetProperty("operationPlan", out var plan)
                && plan.ValueKind == JsonValueKind.Object);
            break;
        }

        Assert.True(received, "Expected at least one SSE event over the wire.");
    }

    [Fact]
    public async Task Subscribe_Should_OmitOperationPlanExtension_When_NotAllowedOverHttp()
    {
        // arrange
        await using var nats = await JetStreamNatsFixture.StartAsync();
        var stream = "S" + Guid.NewGuid().ToString("N");
        var durable = "D" + Guid.NewGuid().ToString("N");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await CreateStreamAsync(nats.Url, stream, Topic, cts.Token);

        using var events = CreateSourceSchema(
            "EVENTS",
            b => b
                .AddQueryType<Events.Query>()
                .AddSubscriptionType<Events.Subscription>()
                .ModifyOptions(o => o.StrictValidation = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("EVENTS", events)],
            configureServices: services => services.AddNatsEventStreamBroker(
                BrokerName,
                o =>
                {
                    o.Url = nats.Url;
                    o.JetStream = new NatsJetStreamOptions { Stream = stream, DurableConsumer = durable };
                }),
            configureGatewayBuilder: b => b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false));

        var httpClient = gateway.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Fusion-Operation-Plan", "1");
        using var client = GraphQLHttpClient.Create(httpClient);

        var request = new OperationRequest(
            """
            subscription {
              onUserCreated {
                user { id }
                cursor
              }
            }
            """);

        // act
        // The Fusion-Operation-Plan header is sent, but the schema disallows operation-plan
        // requests, so the plan must not appear regardless of the header.
        using var response = await client.PostAsync(request, new Uri(GatewayUrl), cts.Token);
        await PublishAsync(nats.Url, Topic, """{"user":{"id":"u1"}}""", cts.Token);

        // assert
        var received = false;
        await foreach (var result in response.ReadAsResultStreamAsync().WithCancellation(cts.Token))
        {
            received = true;
            Assert.True(
                result.Extensions.ValueKind == JsonValueKind.Undefined
                || !result.Extensions.TryGetProperty("fusion", out _));
            break;
        }

        Assert.True(received, "Expected at least one SSE event over the wire.");
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
            Encoding.UTF8.GetBytes(body),
            cancellationToken: cancellationToken);
    }

    public static class Events
    {
        public class Query
        {
            public string Version => "1.0.0";
        }

        public class Subscription
        {
            [EventStream("user { id }", Topic = Topic, Broker = BrokerName)]
            public UserCreated OnUserCreated([EventCursor] string? after)
                => EventStream.Create<UserCreated>(after);
        }

        public record UserCreated(User User, [property: EventCursor] string Cursor);

        public record User(string Id);
    }

    public static class FederatedEvents
    {
        public class Query
        {
            public string Version => "1.0.0";
        }

        public class Subscription
        {
            [EventStream("user { id }", Topic = Topic, Broker = BrokerName)]
            public UserCreated OnUserCreated([EventCursor] string? after)
                => EventStream.Create<UserCreated>(after);
        }

        public record UserCreated(User User, [property: EventCursor] string Cursor);

        [EntityKey("id")]
        public record User(string Id);
    }

    public static class Accounts
    {
        [EntityKey("id")]
        public record User(string Id, string Name);

        public class Query
        {
            [Internal, Lookup]
            public User GetUserById(string id) => new(id, $"User {id}");
        }
    }

    public static class AbstractEntityEvents
    {
        [InterfaceType("Node")]
        public interface INode
        {
            string Id { get; }
        }

        [EntityKey("id")]
        public record Author(string Id) : INode;

        [EntityKey("id")]
        public record Book(string Id) : INode;
    }

    public static class RootAbstractEntityEvents
    {
        public class Query
        {
            public string Version => "1.0.0";
        }

        public class Subscription
        {
            // The abstract payload sits at the ROOT; the body projects only the runtime type and the
            // entity key, and the type-owned fields are resolved per concrete type by the DETAILS lookup.
            [EventStream("__typename id", Topic = Topic, Broker = BrokerName)]
            public AbstractEntityEvents.INode OnNodeCreated([EventCursor] string? after)
                => EventStream.Create<AbstractEntityEvents.INode>(after);
        }
    }

    public static class Details
    {
        [EntityKey("id")]
        public record Author(string Id, string Name);

        [EntityKey("id")]
        public record Book(string Id, string Title);

        public class Query
        {
            [Internal, Lookup]
            public Author GetAuthorById(string id) => new(id, $"Author {id}");

            [Internal, Lookup]
            public Book GetBookById(string id) => new(id, $"Book {id}");
        }
    }

    public static class NestedAbstractEntityEvents
    {
        public class Query
        {
            public string Version => "1.0.0";
        }

        public class Subscription
        {
            // The concrete payload's `item` field is abstract; the body projects only the runtime
            // type and entity key, and the type-owned fields (headline/caption) are resolved per
            // concrete type by the CONTENT lookup.
            [EventStream("item { __typename id }", Topic = Topic, Broker = BrokerName)]
            public ContentCreated OnContentCreated([EventCursor] string? after)
                => EventStream.Create<ContentCreated>(after);
        }

        public record ContentCreated(IItem Item, [property: EventCursor] string Cursor);

        [InterfaceType("Item")]
        public interface IItem
        {
            string Id { get; }
        }

        [EntityKey("id")]
        public record Article(string Id) : IItem;

        [EntityKey("id")]
        public record Video(string Id) : IItem;
    }

    public static class Content
    {
        [EntityKey("id")]
        public record Article(string Id, string Headline);

        [EntityKey("id")]
        public record Video(string Id, string Caption);

        public class Query
        {
            [Internal, Lookup]
            public Article GetArticleById(string id) => new(id, $"Article {id}");

            [Internal, Lookup]
            public Video GetVideoById(string id) => new(id, $"Video {id}");
        }
    }

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

            // The internal TCP-port wait can pass before the mapped host port is connectable, so we
            // establish a real connection (with retries) before handing the broker to the test.
            await fixture.WaitForConnectionAsync();

            return fixture;
        }

        private async Task WaitForConnectionAsync()
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await using var connection = new NatsConnection(new NatsOpts { Url = Url });
                    await connection.ConnectAsync();
                    return;
                }
                catch (NatsException) when (attempt < 20)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250));
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _container.DisposeAsync();
        }
    }
}
