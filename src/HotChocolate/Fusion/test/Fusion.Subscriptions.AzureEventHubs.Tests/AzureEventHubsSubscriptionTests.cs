using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Transport.Formatters;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

public sealed class AzureEventHubsSubscriptionTests
    : IClassFixture<AzureEventHubsFixture>
{
    private readonly AzureEventHubsFixture _fixture;

    public AzureEventHubsSubscriptionTests(AzureEventHubsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Subscribe_Should_DeliverEventWithCrossSchemaData_When_AzureBrokerPublishes()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddGraphQL("EVENTS")
            .AddQueryType<EventsSchema.Query>()
            .AddSourceSchemaDefaults();
        services.AddGraphQL("BOOKS")
            .AddQueryType<BooksSchema.Query>()
            .AddSourceSchemaDefaults();
        services.AddAzureEventHubsEventStreamBroker(
            "azure",
            o =>
            {
                o.ConnectionString = _fixture.ConnectionString;
                o.ConsumerGroup = _fixture.ConsumerGroup;
                o.StartFromEarliest = true;
                o.MaximumWaitTime = TimeSpan.FromSeconds(1);
                o.ConfigureClientOptions = options =>
                {
                    options.RetryOptions.MaximumRetries = 2;
                    options.RetryOptions.TryTimeout = TimeSpan.FromSeconds(10);
                    return options;
                };
            });

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(CreateExecutionSchemaDocument(_fixture.GatewayHub));

        builder.Services.AddSingleton(
            static sp => new InMemorySourceSchemaClientFactory(
                sp.GetRequiredService<IRequestExecutorProvider>(),
                sp.GetRequiredService<IRequestExecutorEvents>(),
                JsonResultFormatter.Default));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            static sp => sp.GetRequiredService<InMemorySourceSchemaClientFactory>());

        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(
                    _ => new InMemorySourceSchemaClientConfiguration("EVENTS"));
                setup.ClientConfigurationModifiers.Add(
                    _ => new InMemorySourceSchemaClientConfiguration("BOOKS"));
            });

        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
        var events = CollectOneEventAsync(executor, cts.Token);

        // act
        await _fixture.PublishAsync(_fixture.GatewayHub, """{"id":1}""", cts.Token);

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

    private static DocumentNode CreateExecutionSchemaDocument(string topic)
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
                  topics: ["{{topic}}"]
                  broker: "azure"
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

    private static class EventsSchema
    {
        public sealed class Query
        {
            public string Foo() => "Foo";
        }
    }

    private static class BooksSchema
    {
        public sealed record Book(int Id, string Title);

        public sealed class Query
        {
            public Book? GetBookById(int id)
                => id == 1 ? new Book(1, "Foo") : null;
        }
    }
}
