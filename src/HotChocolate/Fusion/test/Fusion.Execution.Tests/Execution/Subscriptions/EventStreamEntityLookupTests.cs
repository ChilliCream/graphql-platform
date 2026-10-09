using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Subscriptions;

public sealed class EventStreamEntityLookupTests
{
    [Fact]
    public async Task Subscribe_Should_ResolveBodyViaLookup_When_MessageOnlyCarriesKey()
    {
        // arrange
        // The broker only delivers `review { id }`. `body` is selected beyond the message, so the
        // gateway must run the planned follow-up lookup against the source schema to fill it in.
        var topic = CreateTopic();
        var publisher = new InMemoryEventStreamBrokerHub();
        var lookupClient = new ReviewLookupClient("""{"data":{"reviewById":{"body":"A great read"}}}""");
        var services = CreateServices(topic, publisher, lookupClient);
        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var events = CollectEventsAsync(
            executor,
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
            """,
            count: 1,
            cts.Token);

        await WaitForSubscribersAsync(publisher, topic, count: 1, cts.Token);

        // act
        await publisher.PublishAsync(
            topic,
            CreateMessage("""{"review":{"id":"1"}}"""u8, "Y3Vyc29yLTE="u8),
            cts.Token);

        // assert
        // `body` is non-null: it came from the lookup, not the broker message.
        (await events).Single().MatchInlineSnapshot(
            """
            {
              "data": {
                "onCreateReview": {
                  "review": {
                    "id": "1",
                    "body": "A great read"
                  },
                  "cursor": "Y3Vyc29yLTE="
                }
              }
            }
            """);
    }

    private static ServiceCollection CreateServices(
        string topic,
        InMemoryEventStreamBrokerHub publisher,
        ReviewLookupClient lookupClient)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton(publisher);
        services.AddSingleton<IInMemoryEventStreamPublisher>(publisher);
        services.AddInMemoryEventStreamBroker("memory");

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(CreateExecutionSchemaDocument(topic));

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new ReviewLookupClientFactory(lookupClient));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new ReviewLookupClientConfiguration("EVENTS")));

        return services;
    }

    private static async Task<List<string>> CollectEventsAsync(
        IRequestExecutor executor,
        string document,
        int count,
        CancellationToken cancellationToken)
    {
        var request = OperationRequestBuilder.New()
            .SetDocument(document)
            .Build();

        var result = await executor.ExecuteAsync(request, cancellationToken);
        var stream = result.ExpectResponseStream();
        var events = new List<string>();

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

        await result.DisposeAsync();
        return events;
    }

    private static async Task WaitForSubscribersAsync(
        InMemoryEventStreamBrokerHub publisher,
        string topic,
        int count,
        CancellationToken cancellationToken)
    {
        while (publisher.GetSubscriberCount(topic) < count)
        {
            await Task.Delay(10, cancellationToken);
        }
    }

    private static EventMessage CreateMessage(
        ReadOnlySpan<byte> body,
        ReadOnlySpan<byte> cursor)
    {
        var owner = MemoryPool<byte>.Shared.Rent(body.Length + cursor.Length);
        body.CopyTo(owner.Memory.Span);
        cursor.CopyTo(owner.Memory.Span[body.Length..]);

        return new EventMessage(
            owner,
            0..body.Length,
            body.Length..(body.Length + cursor.Length));
    }

    private static string CreateTopic()
        => "fusion." + Guid.NewGuid().ToString("N");

    private static DocumentNode CreateExecutionSchemaDocument(string topic)
        => Utf8GraphQLParser.Parse(
            $$"""
            schema {
              query: Query
              subscription: Subscription
            }

            type Query
              @fusion__type(schema: EVENTS) {
              reviewById(id: ID!): Review
                @fusion__field(schema: EVENTS)
            }

            type Subscription
              @fusion__type(schema: EVENTS) {
              onCreateReview(after: String): ReviewCreated
                @fusion__field(schema: EVENTS)
                @fusion__eventStream(
                  schema: EVENTS
                  topics: ["{{topic}}"]
                  broker: "memory"
                  message: "review { id }"
                  cursorField: "cursor"
                  cursorArgument: "after"
                )
            }

            type ReviewCreated
              @fusion__type(schema: EVENTS) {
              review: Review!
                @fusion__field(schema: EVENTS)
              cursor: String
                @fusion__field(schema: EVENTS)
            }

            type Review
              @fusion__type(schema: EVENTS)
              @fusion__lookup(
                schema: EVENTS
                key: "{ id }"
                field: "reviewById(id: ID!): Review"
                map: ["id"]
                internal: false
              ) {
              id: ID!
                @fusion__field(schema: EVENTS)
              body: String!
                @fusion__field(schema: EVENTS)
            }

            enum fusion__Schema {
              EVENTS
            }

            scalar fusion__FieldDefinition
            scalar fusion__FieldSelectionMap
            scalar fusion__FieldSelectionSet

            directive @fusion__type(
              schema: fusion__Schema!
            ) repeatable on OBJECT | INTERFACE | UNION | ENUM | INPUT_OBJECT | SCALAR

            directive @fusion__field(
              schema: fusion__Schema!
              sourceName: String
              sourceType: String
              provides: fusion__FieldSelectionSet
              external: Boolean! = false
            ) repeatable on FIELD_DEFINITION

            directive @fusion__lookup(
              schema: fusion__Schema!
              key: fusion__FieldSelectionSet!
              field: fusion__FieldDefinition!
              map: [fusion__FieldSelectionMap!]!
              internal: Boolean! = false
            ) repeatable on OBJECT | INTERFACE

            directive @fusion__eventStream(
              schema: fusion__Schema!
              topics: [String!]
              broker: String
              message: fusion__FieldSelectionSet!
              cursorField: String
              cursorArgument: String
            ) on FIELD_DEFINITION
            """);

    private sealed class ReviewLookupClient(string responseJson) : ISourceSchemaClient
    {
        private readonly byte[] _response = Encoding.UTF8.GetBytes(responseJson);

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, _response, _response.Length);

            // The lookup result is correlated to the entity it enriches via the variable path,
            // mirroring how the real source-schema clients place the merge target.
            var path = request.Variables.Length > 0
                ? request.Variables[0].Path
                : CompactPath.Root;
            var additionalPaths = request.Variables.Length > 0
                ? request.Variables[0].AdditionalPaths
                : default;

            yield return additionalPaths.IsDefaultOrEmpty
                ? new SourceSchemaResult(path, document)
                : new SourceSchemaResult(path, document, additionalPaths: additionalPaths);
        }

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ReviewLookupClientFactory(ISourceSchemaClient client)
        : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is ReviewLookupClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => client;
    }

    private sealed class ReviewLookupClientConfiguration(string name)
        : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.All;
    }
}
