using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class SchemaTransformationIntegrationTests
{
    private static readonly IReadOnlyDictionary<string, EntityRequiresInfo> s_noRequires
        = new Dictionary<string, EntityRequiresInfo>(StringComparer.Ordinal);

    [Fact]
    public async Task Transform_FederationSubgraph_Should_ProduceValidSourceSchema()
    {
        // arrange: build an Apollo Federation subgraph and get its SDL
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<Product>()
            .AddType<User>()
            .BuildSchemaAsync();

        var federationSdl = schema.ToString();

        // act: transform the federation SDL
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(
            result.IsSuccess,
            $"Transform failed: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        var sourceSchemaSdl = result.Value;

        // Should have @key directives
        Assert.Contains("@key", sourceSchemaSdl);

        // Should have @lookup fields
        Assert.Contains("@lookup", sourceSchemaSdl);

        // Should NOT have federation infrastructure
        Assert.DoesNotContain("_entities", sourceSchemaSdl);
        Assert.DoesNotContain("_service", sourceSchemaSdl);
        Assert.DoesNotContain("_Service", sourceSchemaSdl);
        Assert.DoesNotContain("_Entity", sourceSchemaSdl);
        Assert.DoesNotContain("_Any", sourceSchemaSdl);

        // Snapshot the output
        sourceSchemaSdl.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task Rewriter_Should_RewriteLookupToEntities_FromTransformedSchema()
    {
        // arrange: build Federation subgraph, transform, extract lookup fields
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<Product>()
            .BuildSchemaAsync();

        var federationSdl = schema.ToString();
        var result = FederationSchemaTransformer.Transform(federationSdl);

        Assert.True(
            result.IsSuccess,
            $"Transform failed: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        // Parse the source schema SDL to find @lookup fields
        // (In a real scenario, the connector would do this from the MutableSchemaDefinition)
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productById"] = new LookupFieldInfo
            {
                EntityTypeName = "Product",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["id"] = "id" }
            }
        };

        var rewriter = new FederationQueryRewriter(lookupFields, s_noRequires);

        // Simulate what the Fusion planner would generate
        const string plannerQuery = """
            query Op($__fusion_1_id: Int!) {
              productById(id: $__fusion_1_id) {
                id
                name
                price
              }
            }
            """;

        // act
        var rewritten = rewriter.GetOrRewrite(plannerQuery, 42UL);

        // assert
        Assert.True(rewritten.IsEntityLookup);
        Assert.Equal("Product", rewritten.EntityTypeName);
        Assert.Contains("_entities", rewritten.OperationText);
        Assert.Contains("... on Product", rewritten.OperationText);
        Assert.Equal("id", rewritten.VariableToKeyFieldMap["__fusion_1_id"]);

        rewritten.OperationText.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task EntitiesQuery_Should_ResolveEntities_FromFederationSubgraph()
    {
        // arrange: build Federation subgraph executor
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<Product>()
            .BuildRequestExecutorAsync();

        // This is the query our connector would generate after rewriting
        var request = OperationRequestBuilder
            .New()
            .SetDocument(
                """
                query($representations: [_Any!]!) {
                  _entities(representations: $representations) {
                    ... on Product {
                      id
                      name
                      price
                    }
                  }
                }
                """)
            .SetVariableValues(
                """
                {
                  "representations": [
                    { "__typename": "Product", "id": 1 },
                    { "__typename": "Product", "id": 2 }
                  ]
                }
                """)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        result.ToJson().MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task FullRoundtrip_Transform_Rewrite_Execute()
    {
        // 1. Build Federation subgraph
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<Product>()
            .AddType<User>()
            .BuildRequestExecutorAsync();

        // 2. Get and transform the SDL
        var federationSdl = executor.Schema.ToString();
        var transformResult = FederationSchemaTransformer.Transform(federationSdl);

        Assert.True(
            transformResult.IsSuccess,
            $"Transform failed: {string.Join(", ", transformResult.Errors.Select(e => e.Message))}");

        // 3. Set up rewriter with lookup fields extracted from transformed schema
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productById"] = new LookupFieldInfo
            {
                EntityTypeName = "Product",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["id"] = "id" }
            },
            ["userByEmail"] = new LookupFieldInfo
            {
                EntityTypeName = "User",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["email"] = "email" }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields, s_noRequires);

        // 4. Simulate planner query and rewrite
        const string plannerQuery = """
            query($__fusion_1_id: Int!) {
              productById(id: $__fusion_1_id) {
                id
                name
                price
              }
            }
            """;

        var rewritten = rewriter.GetOrRewrite(plannerQuery, 100UL);

        // 5. Execute the rewritten _entities query against the real subgraph
        var request = OperationRequestBuilder
            .New()
            .SetDocument(rewritten.OperationText)
            .SetVariableValues(
                """
                {
                  "representations": [
                    { "__typename": "Product", "id": 1 }
                  ]
                }
                """)
            .Build();

        var result = await executor.ExecuteAsync(request);

        // 6. Verify we got the entity back
        var json = result.ToJson();
        Assert.Contains("Product 1", json);
        json.MatchSnapshot(extension: ".json");
    }

    [Fact]
    public async Task BatchedEntitiesQuery_Should_ResolveMultipleEntityTypes()
    {
        // arrange: build Federation subgraph with Product and User entities
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<Product>()
            .AddType<User>()
            .BuildRequestExecutorAsync();

        // Build a combined aliased query like the connector would
        const string batchedQuery = """
            query($r0: [_Any!]!, $r1: [_Any!]!) {
              ____request0: _entities(representations: $r0) {
                ... on Product { id name price }
              }
              ____request1: _entities(representations: $r1) {
                ... on User { email name }
              }
            }
            """;

        var request = OperationRequestBuilder
            .New()
            .SetDocument(batchedQuery)
            .SetVariableValues(new Dictionary<string, object?>
            {
                ["r0"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["__typename"] = "Product", ["id"] = 1 },
                    new Dictionary<string, object?> { ["__typename"] = "Product", ["id"] = 2 }
                },
                ["r1"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["__typename"] = "User", ["email"] = "test@example.com" }
                }
            })
            .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        var json = result.ToJson();
        Assert.Contains("____request0", json);
        Assert.Contains("____request1", json);
        Assert.Contains("Product 1", json);
        Assert.Contains("Product 2", json);
        Assert.Contains("User test@example.com", json);

        json.MatchSnapshot(extension: ".json");
    }

    [Key("id")]
    [ReferenceResolver(EntityResolver = nameof(ResolveById))]
    public sealed class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public float Price { get; set; }

        public static Product ResolveById(int id)
            => new() { Id = id, Name = $"Product {id}", Price = 9.99f };
    }

    [Key("email")]
    [ReferenceResolver(EntityResolver = nameof(ResolveByEmail))]
    public sealed class User
    {
        public string Email { get; set; } = default!;

        public string Name { get; set; } = default!;

        public static User ResolveByEmail(string email)
            => new() { Email = email, Name = $"User {email}" };
    }

    public class Query
    {
        public Product? GetProduct(int id) => Product.ResolveById(id);

        public List<Product> GetTopProducts()
            => [Product.ResolveById(1), Product.ResolveById(2)];
    }
}
