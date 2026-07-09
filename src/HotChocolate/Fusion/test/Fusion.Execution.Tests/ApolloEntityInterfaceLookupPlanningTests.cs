using HotChocolate.Fusion.Execution.Nodes.Serialization;

namespace HotChocolate.Fusion;

public sealed class ApolloEntityInterfaceLookupPlanningTests : FusionTestBase
{
    // An Apollo Federation subgraph that owns the root 'product' field and the entity interface
    // 'Media'. The entity interface exercises the @key-on-interface path through composition
    // (its key fields must not be stamped @shareable). Product exposes only 'id' here.
    private const string Catalog =
        """
        schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
          query: Query
        }

        type Query {
          product: Product
          media: [Media!]!
          _service: _Service!
          _entities(representations: [_Any!]!): [_Entity]!
        }

        interface Media @key(fields: "id") {
          id: ID!
          title: String!
        }

        type Article implements Media @key(fields: "id") {
          id: ID!
          title: String!
        }

        type Product @key(fields: "id") {
          id: ID!
          name: String!
        }

        type _Service { sdl: String! }

        union _Entity = Product | Article

        scalar FieldSet
        scalar _Any

        directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
        directive @link(url: String! import: [String!]) repeatable on SCHEMA
        """;

    // A second Apollo subgraph that resolves 'Product.reviews'. Because 'reviews' lives only here,
    // reaching it from a Product produced by 'catalog' forces an entity lookup into this subgraph,
    // which must be issued as an Apollo '_entities(representations:)' query rather than a synthetic
    // 'productById' root field (which the subgraph does not expose).
    private const string Reviews =
        """
        schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
          query: Query
        }

        type Query {
          _service: _Service!
          _entities(representations: [_Any!]!): [_Entity]!
        }

        type Product @key(fields: "id") {
          id: ID!
          reviews: [Review!]!
        }

        type Review { body: String! }

        type _Service { sdl: String! }

        union _Entity = Product

        scalar FieldSet
        scalar _Any

        directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
        directive @link(url: String! import: [String!]) repeatable on SCHEMA
        """;

    [Fact]
    public void Plan_Should_Route_Apollo_Entity_Lookup_Through_Entities_Query()
    {
        // arrange
        // Composition succeeds only because the @key on the 'Media' interface no longer stamps its
        // key fields @shareable.
        var schema = ComposeSchema(Catalog, Reviews);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              product {
                reviews { body }
              }
            }
            """);

        var yaml = new YamlOperationPlanFormatter().Format(plan);

        // assert
        // The lookup into the reviews subgraph is issued as an Apollo '_entities' query.
        Assert.Contains("_entities", yaml);
    }
}
