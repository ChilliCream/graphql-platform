using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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
    private const string RecordingBrokerName = "recording";

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

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("\"é\\u0063\"")]
    [InlineData("{ \"number\":1.20e+03,\"zero\":-0.0,\"text\":\"é\\u0063\",\"list\":[true,false,null]}")]
    public async Task Subscribe_Should_PreservePayloadTokens_When_CursorIsInjected(string payload)
    {
        // arrange
        var topic = CreateTopic();
        var publisher = new InMemoryEventStreamBrokerHub();
        await using var services = CreateServices(topic, publisher).BuildServiceProvider();
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(10));
        var events = CollectEventsAsync(
            executor,
            "subscription { onBookChanged { id title payload cursor } }",
            1,
            cancellation.Token);
        await WaitForSubscribersAsync(publisher, topic, 1, cancellation.Token);
        var body = Encoding.UTF8.GetBytes(
            "{\"id\":1,\"title\":\"é\",\"payload\":" + payload + ",\"cursor\":\"stale\"}");

        // act
        await publisher.PublishAsync(topic, CreateMessage(body, "Y3Vyc29yLTE="u8), cancellation.Token);
        var captured = await events;
        using var document = JsonDocument.Parse(captured.Single());
        var data = document.RootElement.GetProperty("data").GetProperty("onBookChanged");

        // assert
        Assert.Equal(1, data.GetProperty("id").GetInt32());
        Assert.Equal("é", data.GetProperty("title").GetString());
        Assert.Equal(ReadPayloadTokens(payload), ReadPayloadTokens(data.GetProperty("payload").GetRawText()));
        Assert.Equal("Y3Vyc29yLTE=", data.GetProperty("cursor").GetString());
    }

    [Fact]
    public async Task Subscribe_Should_PreserveDecodeFailureAndContinue_When_PayloadContainsInvalidUtf8()
    {
        // arrange
        var topic = CreateTopic();
        var publisher = new InMemoryEventStreamBrokerHub();
        await using var services = CreateServices(topic, publisher).BuildServiceProvider();
        var executor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(10));
        using var request = OperationRequestBuilder.New()
            .SetDocument("subscription { onBookChanged { id title cursor } }")
            .Build();
        await using var result = await executor.ExecuteAsync(request, cancellation.Token);
        await using var enumerator = result.ExpectResponseStream().ReadResultsAsync()
            .GetAsyncEnumerator(cancellation.Token);
        byte[] invalidBody = [.. "{\"id\":1,\"title\":\""u8, 0xff, .. "\"}"u8];
        using var legacyDocument = JsonDocument.Parse(invalidBody);
        var expected = Record.Exception(() => legacyDocument.RootElement.GetProperty("title").GetRawText())!;
        var pending = enumerator.MoveNextAsync();
        await WaitForSubscribersAsync(publisher, topic, 1, cancellation.Token);

        // act
        await publisher.PublishAsync(
            topic,
            CreateMessage(invalidBody, "Y3Vyc29yLTE="u8),
            cancellation.Token);
        var failedMoved = await pending;
        await using var failed = enumerator.Current;
        var exception = failed.Errors.Single().Exception!;
        await publisher.PublishAsync(
            topic,
            CreateMessage("{\"id\":2,\"title\":\"Two\"}"u8, "Y3Vyc29yLTI="u8),
            cancellation.Token);
        var nextMoved = await enumerator.MoveNextAsync();
        await using var next = enumerator.Current;

        // assert
        Assert.True(failedMoved);
        Assert.Equal(
            (expected.GetType(), expected.Message, expected.InnerException?.GetType(), expected.InnerException?.Message),
            (exception.GetType(), exception.Message, exception.InnerException?.GetType(), exception.InnerException?.Message));
        Assert.True(nextMoved);
        next.MatchInlineSnapshot(
            """
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

    [Fact]
    public async Task Subscribe_Should_SetRequiresCursorTrue_When_CursorFieldIsConfigured()
    {
        // arrange
        var hub = new RecordingEventStreamBrokerHub();
        var services = CreateRecordingServices(
            CreateRequiresCursorExecutionSchemaDocument(includeCursor: true),
            hub);
        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // act
        await CollectEventsAsync(
            executor,
            """
            subscription {
              onBookChanged {
                id
              }
            }
            """,
            count: 1,
            cts.Token);

        // assert
        Assert.True(hub.RequiresCursor);
    }

    [Fact]
    public async Task Subscribe_Should_SetRequiresCursorFalse_When_CursorFieldIsNotConfigured()
    {
        // arrange
        var hub = new RecordingEventStreamBrokerHub();
        var services = CreateRecordingServices(
            CreateRequiresCursorExecutionSchemaDocument(includeCursor: false),
            hub);
        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // act
        await CollectEventsAsync(
            executor,
            """
            subscription {
              onBookChanged {
                id
              }
            }
            """,
            count: 1,
            cts.Token);

        // assert
        Assert.False(hub.RequiresCursor);
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
        using var request = OperationRequestBuilder.New()
            .SetDocument(document)
            .Build();

        await using var result = await executor.ExecuteAsync(request, cancellationToken);
        var stream = result.ExpectResponseStream();
        var events = new List<string>();

        await foreach (var operationResult in stream
            .ReadResultsAsync()
            .WithCancellation(cancellationToken))
        {
            await using (operationResult)
            {
                events.Add(operationResult.ToJson());

                if (events.Count == count)
                {
                    break;
                }
            }
        }

        return events;
    }

    private static List<(JsonTokenType Type, string Value)> ReadPayloadTokens(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        var tokens = new List<(JsonTokenType Type, string Value)>();
        while (reader.Read())
        {
            tokens.Add((reader.TokenType, Encoding.UTF8.GetString(reader.ValueSpan)));
        }

        return tokens;
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
                  message: "{ id title payload }"
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
              payload: JSON
                @fusion__field(schema: EVENTS)
              cursor: String
                @fusion__field(schema: EVENTS)
            }

            enum fusion__Schema {
              EVENTS
            }

            scalar JSON
              @fusion__type(schema: EVENTS)

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

    private static ServiceCollection CreateRecordingServices(
        DocumentNode executionSchemaDocument,
        RecordingEventStreamBrokerHub hub)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IEventStreamBrokerFactory, DefaultEventStreamBrokerFactory>();
        services.AddKeyedSingleton<IEventStreamBrokerProvider>(
            RecordingBrokerName,
            (_, _) => new RecordingEventStreamBrokerProvider(hub));

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(executionSchemaDocument);

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestSourceSchemaClientFactory());

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestSourceSchemaClientConfiguration("EVENTS")));

        return services;
    }

    private static DocumentNode CreateRequiresCursorExecutionSchemaDocument(bool includeCursor)
    {
        var fieldArguments = includeCursor ? "(after: String)" : "";
        var cursorDirectiveArguments = includeCursor
            ? "cursorField: \"cursor\" cursorArgument: \"after\""
            : "";

        return Utf8GraphQLParser.Parse(
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
              onBookChanged{{fieldArguments}}: BookChanged
                @fusion__field(schema: EVENTS)
                @fusion__eventStream(
                  schema: EVENTS
                  topics: ["events"]
                  broker: "{{RecordingBrokerName}}"
                  message: "{ id }"
                  {{cursorDirectiveArguments}}
                )
            }

            type BookChanged
              @fusion__type(schema: EVENTS) {
              id: Int!
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
    }

    private sealed class RecordingEventStreamBrokerHub
    {
        public bool RequiresCursor { get; private set; }

        public void Capture(ISubscriptionFieldContext context)
            => RequiresCursor = context.RequiresCursor;
    }

    private sealed class RecordingEventStreamBroker(RecordingEventStreamBrokerHub hub) : IEventStreamBroker
    {
        public IAsyncEnumerable<EventMessage> SubscribeAsync(
            ISubscriptionFieldContext context,
            string[] topics,
            string? cursor,
            CancellationToken cancellationToken)
        {
            hub.Capture(context);
            return SubscribeCoreAsync();
        }

        private static async IAsyncEnumerable<EventMessage> SubscribeCoreAsync()
        {
            await Task.Yield();
            yield return CreateMessage("""{"id":1}"""u8, cursor: default);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingEventStreamBrokerProvider(RecordingEventStreamBrokerHub hub)
        : IEventStreamBrokerProvider
    {
        public IEventStreamBroker Create() => new RecordingEventStreamBroker(hub);
    }

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
