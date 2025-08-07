namespace HotChocolate.Fusion.Planning;

public class RequirementTests : FusionTestBase
{
    [Fact]
    public void Plan_Simple_Operation_1_Source_Schema()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema @schemaName(value: "A") {
              query: Query
            }

            type Query {
              books: [Book]
            }

            type Book {
              id: String!
              title: String!
            }
            """,
            """
            schema @schemaName(value: "B") {
              query: Query
            }

            type Query {
              bookById(id: String!): Book @lookup @internal
            }

            type Book {
              id: String!
              titleAndId(title: String @require(field: "title")): String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
                books {
                  titleAndId
                }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requirement_Merged_Into_SelectionSet_With_Non_Lead_Field()
    {
        // arrange
        var schema = ComposeSchema(
            """"
            schema @schemaName(value: "catalog") {
              query: Query
            }

            type Brand {
              id: Int!
              name: String!
            }

            type Product {
              id: Int!
              name: String!
              brand: Brand
            }

            type Query {
              products: [Product]
            }
            """",
            """"
            schema @schemaName(value: "reviews") {
              query: Query
            }

            type Product {
              nameAndId(name: String! @require(field: "name")): String!
              id: Int!
            }

            type Query {
              productById(id: Int!): Product! @lookup @internal
            }
            """");

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                brand { name }
                nameAndId
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }
}
