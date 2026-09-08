using System.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Subscriptions;

/// <summary>
/// Gateway end-to-end coverage for repo-ctf.11: a data-bearing policy on the subscription root's
/// own EventStream payload type, evaluated per event against the composed message projection.
/// </summary>
public sealed class EventStreamPolicyGatewayTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Should_NotCreatePolicyExecutionNode_When_RootPayloadPolicyIsDerivable()
    {
        // arrange
        var topic = CreateTopic();
        var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(
                _ => new TestPolicyProvider(new DenySecretValuePolicy("CanReadBook", "classified")))
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(CreateExecutionSchemaDocument(topic), services);

        // act
        var plan = PlanOperation(
            schema,
            "subscription { bookChanged { id title } }");

        // assert
        Assert.Empty(plan.AllNodes.OfType<HotChocolate.Fusion.Execution.Nodes.PolicyExecutionNode>());
        Assert.Single(plan.PolicySlots);
    }

    [Fact]
    public async Task Subscribe_Should_DeliverAllowedEvent_And_MaskDeniedEvent_When_PayloadPolicyDeniesBySecret()
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
              bookChanged {
                id
                title
              }
            }
            """,
            count: 2,
            cts.Token);

        await WaitForSubscribersAsync(publisher, topic, count: 1, cts.Token);

        // act
        await publisher.PublishAsync(
            topic,
            CreateMessage("""{"id":"1","title":"public book","secret":"public"}"""u8),
            cts.Token);
        await publisher.PublishAsync(
            topic,
            CreateMessage("""{"id":"2","title":"classified book","secret":"classified"}"""u8),
            cts.Token);

        // assert
        string.Join("\n---\n", await events).MatchInlineSnapshot(
            """
            {
              "data": {
                "bookChanged": {
                  "id": "1",
                  "title": "public book"
                }
              }
            }
            ---
            {
              "data": {
                "bookChanged": null
              }
            }
            """);
    }

    [Fact]
    public async Task Subscribe_Should_KeepUpstreamSubscriptionOpen_When_MultipleEventsAreDenied()
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
              bookChanged {
                id
                title
              }
            }
            """,
            count: 3,
            cts.Token);

        await WaitForSubscribersAsync(publisher, topic, count: 1, cts.Token);

        // act
        await publisher.PublishAsync(
            topic,
            CreateMessage("""{"id":"1","title":"classified one","secret":"classified"}"""u8),
            cts.Token);
        await publisher.PublishAsync(
            topic,
            CreateMessage("""{"id":"2","title":"classified two","secret":"classified"}"""u8),
            cts.Token);
        await publisher.PublishAsync(
            topic,
            CreateMessage("""{"id":"3","title":"public again","secret":"public"}"""u8),
            cts.Token);

        // assert
        // Denying two events in a row does not end the stream (this is a per-event data
        // decision, not a request-constant one), and one upstream broker subscription serves
        // every event: no resubscribe happens between the denied and the allowed event.
        Assert.Equal(1, publisher.GetSubscriberCount(topic));
        string.Join("\n---\n", await events).MatchInlineSnapshot(
            """
            {
              "data": {
                "bookChanged": null
              }
            }
            ---
            {
              "data": {
                "bookChanged": null
              }
            }
            ---
            {
              "data": {
                "bookChanged": {
                  "id": "3",
                  "title": "public again"
                }
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
            new ThrowingSourceSchemaClientFactory());
        builder.ConfigureSchemaServices(
            (_, schemaServices) => schemaServices.AddSingleton<IPolicyProvider>(
                new TestPolicyProvider(new DenySecretValuePolicy("CanReadBook", "classified"))));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new ThrowingSourceSchemaClientConfiguration("EVENTS")));

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

    private static EventMessage CreateMessage(ReadOnlySpan<byte> body)
    {
        var owner = MemoryPool<byte>.Shared.Rent(body.Length);
        body.CopyTo(owner.Memory.Span);
        return new EventMessage(owner, 0..body.Length, body.Length..body.Length);
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
              bookChanged: Book
                @fusion__field(schema: EVENTS)
                @fusion__eventStream(
                  schema: EVENTS
                  topics: ["{{topic}}"]
                  broker: "memory"
                  message: "{ id title secret }"
                )
            }

            type Book
              @fusion__type(schema: EVENTS)
              @fusion__policy(names: "CanReadBook") {
              id: ID!
                @fusion__field(schema: EVENTS)
              title: String!
                @fusion__field(schema: EVENTS)
              secret: String!
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
            ) on FIELD_DEFINITION
            """);

    private sealed class DenySecretValuePolicy(string name, string deniedValue) : IPolicy
    {
        private static readonly SelectionSetNode s_requirement =
            Utf8GraphQLParser.Syntax.ParseSelectionSet("{ secret }");

        public string Name => name;

        public PolicyRequirements Requirements { get; } = new() { Resource = s_requirement };

        public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
        {
            var entities = context.Selection!.Entities.Span;

            for (var i = 0; i < entities.Length; i++)
            {
                if (entities[i].TryGetProperty("secret", out var secret)
                    && secret.GetString() == deniedValue)
                {
                    context.Deny(i, "denied by test policy");
                }
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingSourceSchemaClientFactory : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is ThrowingSourceSchemaClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => new ThrowingSourceSchemaClient();
    }

    private sealed class ThrowingSourceSchemaClientConfiguration(string name)
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
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
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
            System.Collections.Immutable.ImmutableArray<SourceSchemaClientRequest> requests,
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
