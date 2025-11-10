namespace HotChocolate.Fusion.Planning;

public class LookupTests : FusionTestBase
{
    [Fact]
    public void Nested_Lookup()
    {
        // arrange
        var schema = ComposeSchema(
            """"
            schema {
              query: Query
            }

            type Query {
              products: [Product]
            }

            type Brand @key(fields: "id") {
              id: Int!
            }

            type Product @key(fields: "id") {
              id: Int!
              name: String!
              brand: Brand
            }
            """",
            """"
            schema {
              query: Query
            }

            type Query {
              lookups : InternalLookups! @internal
            }

            type InternalLookups @internal {
              brandById(id: Int!): Brand! @lookup
            }

            type Brand @key(fields: "id") {
              id: Int!
              name: String!
            }
            """");

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                brand { name }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }
}
