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

namespace HotChocolate.Fusion;

public sealed class SupertypeNarrowingExecutionTests
{
    [Fact]
    public async Task Execute_Should_ReturnReviewResult_When_NarrowingSourceCannotCoverRequestedUnionMember()
    {
        // arrange
        var clients = new Dictionary<string, TestSourceSchemaClient>
        {
            ["A"] = new("""{"data":{"featured":{"__typename":"Review","rating":5}}}"""),
            ["B"] = new("""{"data":{"featured":{"__typename":"Product","name":"Product from B"}}}""", failOnExecute: true)
        };

        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(CreateExecutionSchemaDocument());

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestSourceSchemaClientFactory(clients));

        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(_ => new TestSourceSchemaClientConfiguration("A"));
                setup.ClientConfigurationModifiers.Add(_ => new TestSourceSchemaClientConfiguration("B"));
            });

        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            query {
              featured {
                __typename
                ... on Product {
                  name
                }
                ... on Review {
                  rating
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(clients["A"].Requests);
        Assert.Empty(clients["B"].Requests);
        result.MatchMarkdownSnapshot();
    }

    private static DocumentNode CreateExecutionSchemaDocument()
        => Utf8GraphQLParser.Parse(
            """
            type Query
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              featured: FeaturedItem
                @fusion__field(schema: B, sourceType: "Product!")
                @fusion__field(schema: A)
            }

            type Product
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              name: String!
                @fusion__field(schema: B)
                @fusion__field(schema: A)
            }

            type Review
              @fusion__type(schema: A) {
              rating: Int!
                @fusion__field(schema: A)
            }

            union FeaturedItem
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Product")
              @fusion__unionMember(schema: A, member: "Review") = Product | Review

            enum fusion__Schema {
              A
              B
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

            directive @fusion__unionMember(
              schema: fusion__Schema!
              member: String!
            ) repeatable on UNION
            """);

    private sealed class TestSourceSchemaClient(string response, bool failOnExecute = false)
        : ISourceSchemaClient
    {
        private readonly byte[] _response = Encoding.UTF8.GetBytes(response);
        private readonly List<SourceSchemaClientRequest> _requests = [];

        public IReadOnlyList<SourceSchemaClientRequest> Requests => _requests;

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (failOnExecute)
            {
                throw new InvalidOperationException(
                    $"The source schema '{request.SchemaName}' should not have been executed.");
            }

            _requests.Add(request);

            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, _response, _response.Length);

            yield return new SourceSchemaResult(CompactPath.Root, document);

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

        public SupportedOperationType SupportedOperations => SupportedOperationType.Query;
    }
}
