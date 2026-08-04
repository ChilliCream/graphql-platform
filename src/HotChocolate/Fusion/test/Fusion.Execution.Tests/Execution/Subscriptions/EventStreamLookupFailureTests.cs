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

public sealed class EventStreamLookupFailureTests
{
    [Fact]
    public async Task Subscribe_Should_EmitErrorResultAndStayAlive_When_EnrichmentLookupFails()
    {
        // arrange
        // The first event's enrichment lookup returns a subgraph error + null entity; the second
        // event's lookup succeeds. A failed enrichment must surface as a per-event error result
        // (subgraph error + standard non-null propagation) and the subscription must stay alive so
        // the following event is still delivered.
        var topic = CreateTopic();
        var publisher = new InMemoryEventStreamBrokerHub();
        var lookupClient = new SequencedLookupClient(
            """{"data":{"reviewById":null},"errors":[{"message":"The node ID string has an invalid format.","path":["reviewById"]}]}""",
            """{"data":{"reviewById":{"body":"A great read"}}}""");
        var services = CreateServices(topic, publisher, lookupClient);
        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

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
            count: 2,
            cts.Token);

        await WaitForSubscribersAsync(publisher, topic, count: 1, cts.Token);

        // act
        await publisher.PublishAsync(
            topic,
            CreateMessage("""{"review":{"id":"1"}}"""u8, "Y3Vyc29yLTE="u8),
            cts.Token);
        await publisher.PublishAsync(
            topic,
            CreateMessage("""{"review":{"id":"2"}}"""u8, "Y3Vyc29yLTI="u8),
            cts.Token);

        // assert
        var results = await events;
        Assert.Equal(2, results.Count);
        results.MatchInlineSnapshots(
        [
            """
            {
              "errors": [
                {
                  "message": "The node ID string has an invalid format.",
                  "path": [
                    "onCreateReview",
                    "review",
                    "body"
                  ]
                }
              ],
              "data": null
            }
            """,
            """
            {
              "data": {
                "onCreateReview": {
                  "review": {
                    "id": "2",
                    "body": "A great read"
                  },
                  "cursor": "Y3Vyc29yLTI="
                }
              }
            }
            """
        ]);
    }

    private static ServiceCollection CreateServices(
        string topic,
        InMemoryEventStreamBrokerHub publisher,
        SequencedLookupClient lookupClient)
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
            new SequencedLookupClientFactory(lookupClient));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new SequencedLookupClientConfiguration("EVENTS")));

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
              onCreateReview(after: String): ReviewCreated!
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

    // Returns each queued response in order, one per ExecuteAsync call.
    private sealed class SequencedLookupClient(params string[] responses) : ISourceSchemaClient
    {
        private readonly byte[][] _responses = [.. responses.Select(Encoding.UTF8.GetBytes)];
        private int _index = -1;

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            var response = _responses[Math.Min(Interlocked.Increment(ref _index), _responses.Length - 1)];
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, response, response.Length);

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

    private sealed class SequencedLookupClientFactory(ISourceSchemaClient client)
        : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is SequencedLookupClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => client;
    }

    private sealed class SequencedLookupClientConfiguration(string name)
        : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.All;
    }
}
