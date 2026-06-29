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
using NATS.Client.Core;
using Squadron;

namespace HotChocolate.Fusion.Subscriptions.NATS;

public sealed class NatsSubscriptionTests : IClassFixture<NatsResource>
{
    private readonly NatsResource _natsResource;

    public NatsSubscriptionTests(NatsResource natsResource)
    {
        _natsResource = natsResource;
    }

    [Fact]
    public async Task Subscribe_Should_DeliverEventWithCrossSchemaData_When_NatsBrokerPublishes()
    {
        // arrange
        var subject = "fusion." + Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddNatsEventStreamBroker("nats", o =>
            o.Url = _natsResource.NatsConnectionString);

        var clients = new Dictionary<string, TestSourceSchemaClient>
        {
            ["EVENTS"] = new("""{"data":{}}""", failOnExecute: true),
            ["BOOKS"] = new(
                request =>
                    """{"data":{"{0}":{"id":1,"title":"Foo"}}}"""
                        .Replace("{0}", GetRootResponseName(request), StringComparison.Ordinal))
        };

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(CreateExecutionSchemaDocument(subject));

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestSourceSchemaClientFactory(clients));

        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(
                    _ => new TestSourceSchemaClientConfiguration("EVENTS"));
                setup.ClientConfigurationModifiers.Add(
                    _ => new TestSourceSchemaClientConfiguration("BOOKS"));
            });

        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var events = CollectOneEventAsync(executor, cts.Token);
        await Task.Delay(500, cts.Token);

        // act
        await using var pubConn = new NatsConnection(
            new NatsOpts { Url = _natsResource.NatsConnectionString });
        await pubConn.PublishAsync(subject, """{"id":1}"""u8.ToArray(), cancellationToken: cts.Token);

        // assert
        var results = await events;

        string.Join("\n---\n", results).MatchMarkdownSnapshot();
    }

    private static async Task<List<string>> CollectOneEventAsync(
        IRequestExecutor executor,
        CancellationToken cancellationToken)
    {
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                subscription {
                  onBookChanged {
                    id
                    title
                  }
                }
                """)
            .Build();

        var result = await executor.ExecuteAsync(request, cancellationToken);
        var stream = result.ExpectResponseStream();
        var events = new List<string>();

        await foreach (var operationResult in stream
            .ReadResultsAsync()
            .WithCancellation(cancellationToken))
        {
            events.Add(operationResult.ToJson());
            break;
        }

        await result.DisposeAsync();
        return events;
    }

    private static DocumentNode CreateExecutionSchemaDocument(string subject)
        => Utf8GraphQLParser.Parse(
            $$"""
            schema {
              query: Query
              subscription: Subscription
            }

            type Query
              @fusion__type(schema: BOOKS) {
              bookById(id: Int!): Book
                @fusion__field(schema: BOOKS)
            }

            type Subscription
              @fusion__type(schema: EVENTS) {
              onBookChanged: Book
                @fusion__field(schema: EVENTS)
                @fusion__eventStream(
                  schema: EVENTS
                  topics: ["{{subject}}"]
                  broker: "nats"
                  message: "{ id }"
                )
            }

            type Book
              @fusion__type(schema: EVENTS)
              @fusion__type(schema: BOOKS)
              @fusion__lookup(
                schema: BOOKS
                key: "{ id }"
                field: "bookById(id: Int!): Book"
                map: ["id"]
                internal: false
              ) {
              id: Int!
                @fusion__field(schema: EVENTS)
                @fusion__field(schema: BOOKS)
              title: String!
                @fusion__field(schema: BOOKS)
            }

            enum fusion__Schema {
              EVENTS
              BOOKS
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
            ) on FIELD_DEFINITION
            """);

    private static string GetRootResponseName(SourceSchemaClientRequest request)
    {
        var document = Utf8GraphQLParser.Parse(request.OperationSourceText);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var field = operation.SelectionSet.Selections.OfType<FieldNode>().Single();

        return field.Alias?.Value ?? field.Name.Value;
    }

    private sealed class TestSourceSchemaClient : ISourceSchemaClient
    {
        private readonly Func<SourceSchemaClientRequest, string> _createResponse;
        private readonly bool _failOnExecute;
        private readonly List<SourceSchemaClientRequest> _requests = [];

        public TestSourceSchemaClient(string response, bool failOnExecute = false)
            : this(_ => response, failOnExecute)
        {
        }

        public TestSourceSchemaClient(
            Func<SourceSchemaClientRequest, string> createResponse,
            bool failOnExecute = false)
        {
            _createResponse = createResponse;
            _failOnExecute = failOnExecute;
        }

        public IReadOnlyList<SourceSchemaClientRequest> Requests => _requests;

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_failOnExecute)
            {
                throw new InvalidOperationException(
                    $"The source schema '{request.SchemaName}' should not have been executed.");
            }

            _requests.Add(request);

            var response = Encoding.UTF8.GetBytes(_createResponse(request));
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, response, response.Length);

            var variable = request.Variables.IsDefaultOrEmpty
                ? VariableValues.Empty
                : request.Variables[0];

            yield return variable.AdditionalPaths.IsDefaultOrEmpty
                ? new SourceSchemaResult(variable.Path, document)
                : new SourceSchemaResult(variable.Path, document, additionalPaths: variable.AdditionalPaths);

            await Task.Yield();
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

    private sealed class TestSourceSchemaClientFactory(
        IReadOnlyDictionary<string, TestSourceSchemaClient> clients)
        : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is TestSourceSchemaClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => clients[configuration.Name];
    }

    private sealed class TestSourceSchemaClientConfiguration(string name)
        : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.All;
    }
}
