using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class PropagateNullLookupExecutionTests
{
    [Fact]
    public async Task Execute_Should_NullEntityAndSkipDependentLookup_When_PropagateNullLookupReturnsCleanNull()
    {
        // arrange
        var clients = new TestSourceSchemaClients(
            ("A", """{"data":{"product":{"id":"1","name":"Chair"}}}"""),
            ("B", """{"data":{"productById":null}}"""));
        var executor = await CreateExecutorAsync(clients, nullableProduct: true, listProduct: false);

        // act
        var result = await executor.ExecuteAsync(
            """
            query {
              product {
                name
                description
                detail
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(clients.RequestsBySchema("A"));
        Assert.Single(clients.RequestsBySchema("B"));
        Assert.Empty(clients.RequestsBySchema("C"));
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "product": null
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_LiftLookupErrorToEntityPath_When_PropagateNullLookupReturnsNullWithError()
    {
        // arrange
        var clients = new TestSourceSchemaClients(
            ("A", """{"data":{"product":{"id":"1","name":"Chair"}}}"""),
            ("B", """{"data":{"productById":null},"errors":[{"message":"Product missing.","path":["productById"]}]}"""));
        var executor = await CreateExecutorAsync(clients, nullableProduct: true, listProduct: false);

        // act
        var result = await executor.ExecuteAsync(
            """
            query {
              product {
                name
                description
                detail
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(clients.RequestsBySchema("A"));
        Assert.Single(clients.RequestsBySchema("B"));
        Assert.Empty(clients.RequestsBySchema("C"));
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Product missing.",
                  "path": [
                    "product"
                  ]
                }
              ],
              "data": {
                "product": null
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_BubbleFromEntity_When_CleanPropagateNullLookupInvalidatesNonNullEntity()
    {
        // arrange
        var clients = new TestSourceSchemaClients(
            ("A", """{"data":{"product":{"id":"1","name":"Chair"}}}"""),
            ("B", """{"data":{"productById":null}}"""));
        var executor = await CreateExecutorAsync(clients, nullableProduct: false, listProduct: false);

        // act
        var result = await executor.ExecuteAsync(
            """
            query {
              product {
                name
                description
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(clients.RequestsBySchema("A"));
        Assert.Single(clients.RequestsBySchema("B"));
        Assert.Empty(clients.RequestsBySchema("C"));
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Cannot return null for non-nullable field.",
                  "path": [
                    "product"
                  ],
                  "extensions": {
                    "code": "HC0018"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_NullOnlyListElementAndResolveSibling_When_PropagateNullLookupReturnsNullInList()
    {
        // arrange
        var clients = new TestSourceSchemaClients(
            ("A", """{"data":{"products":[{"id":"1","name":"Chair"},{"id":"2","name":"Desk"}]}}"""),
            ("B", """{"data":{"productById":null}}"""),
            ("B", """{"data":{"productById":{"description":"desk-description"}}}"""),
            ("C", """{"data":{"productByDescription":{"detail":"Ships tomorrow"}}}"""));
        var executor = await CreateExecutorAsync(clients, nullableProduct: true, listProduct: true);

        // act
        var result = await executor.ExecuteAsync(
            """
            query {
              products {
                name
                description
                detail
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(clients.RequestsBySchema("A"));
        Assert.Single(clients.RequestsBySchema("B"));
        var cRequest = Assert.Single(clients.RequestsBySchema("C"));
        Assert.Equal(1, cRequest.VariableSetCount);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  null,
                  {
                    "name": "Desk",
                    "description": "desk-description",
                    "detail": "Ships tomorrow"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_SkipKeyOnlyDownstreamLookup_When_PropagateNullLookupInvalidatesEntity()
    {
        // arrange
        var clients = new TestSourceSchemaClients(
            ("A", """{"data":{"product":{"id":"1","name":"Chair"}}}"""),
            ("B", """{"data":{"productById":null}}"""),
            ("C", """{"data":{"productDetailsById":{"detail":"Ships tomorrow"}}}"""));
        var executor = await CreateExecutorAsync(
            clients,
            nullableProduct: true,
            listProduct: false,
            downstreamLookupUsesEntityKey: true);

        // act
        var result = await executor.ExecuteAsync(
            """
            query {
              product {
                name
                description
                detail
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(clients.RequestsBySchema("A"));
        Assert.Single(clients.RequestsBySchema("B"));
        Assert.Empty(clients.RequestsBySchema("C"));
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "product": null
              }
            }
            """);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        TestSourceSchemaClients clients,
        bool nullableProduct,
        bool listProduct,
        bool downstreamLookupUsesEntityKey = false)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                CreateExecutionSchemaDocument(
                    nullableProduct,
                    listProduct,
                    downstreamLookupUsesEntityKey));

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestSourceSchemaClientFactory(clients));

        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(_ => new TestSourceSchemaClientConfiguration("A"));
                setup.ClientConfigurationModifiers.Add(_ => new TestSourceSchemaClientConfiguration("B"));
                setup.ClientConfigurationModifiers.Add(_ => new TestSourceSchemaClientConfiguration("C"));
            });

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static DocumentNode CreateExecutionSchemaDocument(
        bool nullableProduct,
        bool listProduct,
        bool downstreamLookupUsesEntityKey)
    {
        var productFieldType = listProduct
            ? "[Product]"
            : nullableProduct
                ? "Product"
                : "Product!";

        var productFieldName = listProduct ? "products" : "product";
        var downstreamLookupFieldDefinition = downstreamLookupUsesEntityKey
            ? "productDetailsById(id: ID!): Product"
            : "productByDescription(description: String!): Product";
        var downstreamLookupMap = downstreamLookupUsesEntityKey
            ? "id"
            : "description";

        return Utf8GraphQLParser.Parse(
            $$"""
            type Query
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__type(schema: C) {
              {{productFieldName}}: {{productFieldType}}
                @fusion__field(schema: A)
              productById(id: ID!): Product
                @fusion__field(schema: B)
              {{downstreamLookupFieldDefinition}}
                @fusion__field(schema: C)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__type(schema: C)
              @fusion__lookup(
                schema: B
                key: "{ id }"
                field: "productById(id: ID!): Product"
                map: ["id"]
                propagateNull: true
                internal: false
              )
              @fusion__lookup(
                schema: C
                key: "{ {{downstreamLookupMap}} }"
                field: "{{downstreamLookupFieldDefinition}}"
                map: ["{{downstreamLookupMap}}"]
                internal: false
              ) {
              id: ID!
                @fusion__field(schema: A)
              name: String
                @fusion__field(schema: A)
              description: String!
                @fusion__field(schema: B)
              detail: String
                @fusion__field(schema: C)
            }

            enum fusion__Schema {
              A
              B
              C
            }

            scalar fusion__FieldDefinition
            scalar fusion__FieldSelectionMap
            scalar fusion__FieldSelectionPath
            scalar fusion__FieldSelectionSet

            directive @fusion__type(
              schema: fusion__Schema!
            ) repeatable on OBJECT | INTERFACE | UNION | ENUM | INPUT_OBJECT | SCALAR

            directive @fusion__field(
              schema: fusion__Schema!
            ) repeatable on FIELD_DEFINITION

            directive @fusion__lookup(
              schema: fusion__Schema!
              key: fusion__FieldSelectionSet!
              field: fusion__FieldDefinition!
              map: [fusion__FieldSelectionMap!]!
              path: fusion__FieldSelectionPath
              propagateNull: Boolean! = false
              internal: Boolean! = false
            ) repeatable on OBJECT | INTERFACE | UNION
            """);
    }

    private sealed class TestSourceSchemaClients(params (string SchemaName, string Response)[] responses)
        : ISourceSchemaClient
    {
        private readonly Dictionary<string, Queue<byte[]>> _responses = responses
            .GroupBy(t => t.SchemaName, StringComparer.Ordinal)
            .ToDictionary(
                t => t.Key,
                t => new Queue<byte[]>(t.Select(r => Encoding.UTF8.GetBytes(r.Response))),
                StringComparer.Ordinal);
        private readonly List<RequestInfo> _requests = [];

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public IReadOnlyList<RequestInfo> RequestsBySchema(string schemaName)
            => [.. _requests.Where(t => t.SchemaName.Equals(schemaName, StringComparison.Ordinal))];

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            _requests.Add(new RequestInfo(request.SchemaName, request.Variables.Length));

            if (request.Variables.Length == 0)
            {
                yield return CreateResult(context, request.SchemaName, CompactPath.Root);
                yield break;
            }

            foreach (var variable in request.Variables)
            {
                yield return variable.AdditionalPaths.IsDefaultOrEmpty
                    ? CreateResult(context, request.SchemaName, variable.Path)
                    : CreateResult(context, request.SchemaName, variable.Path, variable.AdditionalPaths);
            }
        }

        public async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();

            for (var i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                _requests.Add(new RequestInfo(request.SchemaName, request.Variables.Length));

                if (request.Variables.Length == 0)
                {
                    yield return new SourceSchemaBatchResult(
                        i,
                        CreateResult(context, request.SchemaName, CompactPath.Root));
                    continue;
                }

                foreach (var variable in request.Variables)
                {
                    var result = variable.AdditionalPaths.IsDefaultOrEmpty
                        ? CreateResult(context, request.SchemaName, variable.Path)
                        : CreateResult(context, request.SchemaName, variable.Path, variable.AdditionalPaths);

                    yield return new SourceSchemaBatchResult(i, result);
                }
            }
        }

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private SourceSchemaResult CreateResult(
            OperationPlanContext context,
            string schemaName,
            CompactPath path,
            CompactPathSegment additionalPaths = default)
        {
            if (!_responses.TryGetValue(schemaName, out var responses) || responses.Count == 0)
            {
                throw new InvalidOperationException($"No response configured for schema `{schemaName}`.");
            }

            var response = responses.Dequeue();
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, response, response.Length);

            return additionalPaths.IsDefaultOrEmpty
                ? new SourceSchemaResult(path, document)
                : new SourceSchemaResult(path, document, additionalPaths: additionalPaths);
        }
    }

    private sealed record RequestInfo(string SchemaName, int VariableSetCount);

    private sealed class TestSourceSchemaClientFactory(TestSourceSchemaClients clients)
        : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is TestSourceSchemaClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => clients;
    }

    private sealed class TestSourceSchemaClientConfiguration(string name)
        : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.All;
    }
}
