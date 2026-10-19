namespace HotChocolate.Fusion.Planning;

public sealed class ApolloLookupMergePlanningTests : FusionTestBase
{
    private const string Catalog =
        """
        extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Query {
          products: [Product!]!
          brands: [Brand!]!
        }

        type Product @key(fields: "id") {
          id: ID!
        }

        type Brand @key(fields: "id") {
          id: ID!
          topProduct: Product!
        }
        """;

    private const string Featured =
        """
        extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Brand @key(fields: "id") {
          id: ID!
          featured: Product!
        }

        type Product @key(fields: "id") {
          id: ID!
        }
        """;

    private const string Details =
        """
        extend schema @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

        type Product @key(fields: "id") {
          id: ID!
          name: String!
        }
        """;

    [Fact]
    public void Plan_Should_MergeApolloLookups_When_IdenticalLookupsShareDepth()
    {
        // arrange
        var schema = ComposeSchema(Catalog, Featured, Details);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                name
              }
              brands {
                topProduct {
                  name
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepApolloLookupsSeparate_When_IdenticalLookupsDifferInDepth()
    {
        // arrange
        var schema = ComposeSchema(Catalog, Featured, Details);

        // act
        // 'brands.featured' is only known after the Featured lookup, so its Details
        // lookup sits one level deeper than the one for 'products'
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                name
              }
              brands {
                featured {
                  name
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }
}
