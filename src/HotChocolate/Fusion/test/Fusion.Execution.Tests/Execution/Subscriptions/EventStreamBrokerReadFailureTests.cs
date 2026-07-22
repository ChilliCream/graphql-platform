using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Fusion.Subscriptions;

public sealed class EventStreamBrokerReadFailureTests
{
    private const string BrokerName = "failing";

    [Fact]
    public async Task Subscribe_Should_EndStreamAndRaiseSubscriptionEventError_When_BrokerReadFails()
    {
        // arrange
        // The broker delivers one event and then fails the next read with a connection
        // loss. The failed read must be reported through SubscriptionEventError and end
        // the subscription stream gracefully instead of surfacing a raw exception.
        var topic = CreateTopic();
        var broker = new FailingEventStreamBroker();
        var listener = new CapturingDiagnosticListener();
        var services = CreateServices(topic, broker, listener);
        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var events = CollectAllEventsAsync(
            executor,
            """
            subscription {
              onBookChanged {
                id
              }
            }
            """,
            cts.Token);

        await WaitForSubscriberAsync(broker, cts.Token);

        // act
        await broker.PublishAsync("""{"id":1}"""u8.ToArray(), cts.Token);
        broker.Fail(new InvalidOperationException(
            "The connection to the event stream broker was lost."));

        // assert
        // the delivered event arrives, then the stream completes without throwing
        var results = await events;
        results.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "onBookChanged": {
                  "id": 1
                }
              }
            }
            """
        ]);

        var exception = Assert.IsType<InvalidOperationException>(listener.Exception);
        Assert.Equal("The connection to the event stream broker was lost.", exception.Message);
        Assert.Equal("EVENTS", listener.SchemaName);
    }

    private static ServiceCollection CreateServices(
        string topic,
        FailingEventStreamBroker broker,
        CapturingDiagnosticListener listener)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.TryAddSingleton<IEventStreamBrokerFactory, DefaultEventStreamBrokerFactory>();
        services.AddKeyedSingleton<IEventStreamBrokerProvider>(
            BrokerName,
            (_, _) => new FailingEventStreamBrokerProvider(broker));

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(CreateExecutionSchemaDocument(topic))
            .AddDiagnosticEventListener(_ => listener);

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestSourceSchemaClientFactory());

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestSourceSchemaClientConfiguration("EVENTS")));

        return services;
    }

    private static async Task<List<string>> CollectAllEventsAsync(
        IRequestExecutor executor,
        string document,
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
        }

        await result.DisposeAsync();
        return events;
    }

    private static async Task WaitForSubscriberAsync(
        FailingEventStreamBroker broker,
        CancellationToken cancellationToken)
    {
        while (broker.SubscriberCount < 1)
        {
            await Task.Delay(10, cancellationToken);
        }
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
              onBookChanged: BookChanged
                @fusion__field(schema: EVENTS)
                @fusion__eventStream(
                  schema: EVENTS
                  topics: ["{{topic}}"]
                  broker: "{{BrokerName}}"
                  message: "{ id }"
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

    // Delivers published events in order and then surfaces the injected failure from the
    // read, modeling a broker connection loss while the subscription awaits the next event.
    private sealed class FailingEventStreamBroker : IEventStreamBroker
    {
        private readonly Channel<EventMessage> _channel = Channel.CreateUnbounded<EventMessage>();
        private int _subscriberCount;

        public int SubscriberCount => Volatile.Read(ref _subscriberCount);

        public async ValueTask PublishAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
        {
            var owner = MemoryPool<byte>.Shared.Rent(body.Length);
            body.Span.CopyTo(owner.Memory.Span);
            var message = new EventMessage(owner, 0..body.Length, body.Length..body.Length);
            await _channel.Writer.WriteAsync(message, cancellationToken);
        }

        public void Fail(Exception exception)
            => _channel.Writer.TryComplete(exception);

        public IAsyncEnumerable<EventMessage> SubscribeAsync(
            ISubscriptionFieldContext context,
            string[] topics,
            string? cursor,
            CancellationToken cancellationToken)
            => SubscribeCoreAsync(cancellationToken);

        private async IAsyncEnumerable<EventMessage> SubscribeCoreAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _subscriberCount);

            await foreach (var message in _channel.Reader
                .ReadAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return message;
            }
        }

        public ValueTask DisposeAsync()
        {
            _channel.Writer.TryComplete();

            while (_channel.Reader.TryRead(out var message))
            {
                message.Dispose();
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class FailingEventStreamBrokerProvider(FailingEventStreamBroker broker)
        : IEventStreamBrokerProvider
    {
        public IEventStreamBroker Create() => broker;
    }

    private sealed class CapturingDiagnosticListener : FusionExecutionDiagnosticEventListener
    {
        public string? SchemaName { get; private set; }

        public Exception? Exception { get; private set; }

        public override void SubscriptionEventError(
            OperationPlanContext context,
            ExecutionNode node,
            string schemaName,
            ulong subscriptionId,
            Exception exception)
        {
            SchemaName = schemaName;
            Exception = exception;
        }
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

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
