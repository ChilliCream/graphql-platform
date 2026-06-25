using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Subscriptions;

public sealed class EventStreamCursorTests
{
    [Fact]
    public async Task Subscribe_Should_EmitBase64CursorPerEvent_When_BrokerProvidesCursor()
    {
        // arrange
        var topic = CreateTopic();
        var publisher = new InMemoryEventStreamBrokerHub();
        var services = CreateServices(topic, publisher);
        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var events = CollectEventsAsync(
            executor,
            """
            subscription {
              onBookChanged {
                id
                title
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
            CreateMessage("""{"id":1,"title":"One"}"""u8, "Y3Vyc29yLTE="u8),
            cts.Token);
        await publisher.PublishAsync(
            topic,
            CreateMessage("""{"id":2,"title":"Two"}"""u8, "Y3Vyc29yLTI="u8),
            cts.Token);

        // assert
        string.Join("\n---\n", await events).MatchInlineSnapshot(
            """
            {
              "data": {
                "onBookChanged": {
                  "id": 1,
                  "title": "One",
                  "cursor": "Y3Vyc29yLTE="
                }
              }
            }
            ---
            {
              "data": {
                "onBookChanged": {
                  "id": 2,
                  "title": "Two",
                  "cursor": "Y3Vyc29yLTI="
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Subscribe_Should_Error_When_BrokerRejectsCursor()
    {
        // arrange
        var topic = CreateTopic();
        var publisher = new InMemoryEventStreamBrokerHub();
        var services = CreateServices(topic, publisher);
        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // act
        var events = await CollectEventsAsync(
            executor,
            """
            subscription {
              onBookChanged(after: "not-base64") {
                id
                cursor
              }
            }
            """,
            count: 1,
            cts.Token);

        // assert
        Assert.Equal(0, publisher.GetSubscriberCount(topic));
        events.Single().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The cursor is invalid.",
                  "path": [
                    "onBookChanged"
                  ]
                }
              ],
              "data": {
                "onBookChanged": null
              }
            }
            """);
    }

    private static ServiceCollection CreateServices(
        string topic,
        InMemoryEventStreamBrokerHub publisher)
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
            new TestSourceSchemaClientFactory());

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestSourceSchemaClientConfiguration("EVENTS")));

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
              field: String
                @fusion__field(schema: EVENTS)
            }

            type Subscription
              @fusion__type(schema: EVENTS) {
              onBookChanged(after: String): BookChanged
                @fusion__field(schema: EVENTS)
                @fusion__eventStream(
                  schema: EVENTS
                  topics: ["{{topic}}"]
                  broker: "memory"
                  message: "{ id title }"
                  cursorField: "cursor"
                  cursorArgument: "after"
                )
            }

            type BookChanged
              @fusion__type(schema: EVENTS) {
              id: Int!
                @fusion__field(schema: EVENTS)
              title: String!
                @fusion__field(schema: EVENTS)
              cursor: String
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

            directive @fusion__eventStream(
              schema: fusion__Schema!
              topics: [String!]
              broker: String
              message: fusion__FieldSelectionSet!
              cursorField: String
              cursorArgument: String
            ) on FIELD_DEFINITION
            """);

    private sealed class TestSourceSchemaClientFactory : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is TestSourceSchemaClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => new ThrowingSourceSchemaClient();
    }

    private sealed class TestSourceSchemaClientConfiguration(string name)
        : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.All;
    }

    private sealed class ThrowingSourceSchemaClient : ISourceSchemaClient
    {
        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new InvalidOperationException(
                $"The source schema '{request.SchemaName}' should not have been executed.");
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
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

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
