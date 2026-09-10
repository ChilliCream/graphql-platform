using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class SchemaTransformationIntegrationTests
{
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
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

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
    public async Task EntitiesQuery_Should_ResolveEntities_FromFederationSubgraph()
    {
        // arrange: build Federation subgraph executor
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<Product>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

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
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot(extension: ".json");
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
