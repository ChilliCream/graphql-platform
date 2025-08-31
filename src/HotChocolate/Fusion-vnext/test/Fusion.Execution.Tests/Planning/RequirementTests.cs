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

    [Fact]
    public void Requirement_SelectionMap_Object()
    {
        // arrange
        var schema = ComposeSchema(
            """"
            schema {
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
            schema {
              query: Query
            }

            type Product {
              nameAndId(
                input: NameInput
                  @require(field:
                    """
                    {
                      name
                      brandName: brand.name
                    }
                    """)): String!
              id: Int!
            }

            type Query {
              productById(id: Int!): Product! @lookup @internal
            }

            input NameInput {
              name: String!
              brandName: String
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

    [Fact]
    public void Requirement_SelectionMap_Object_Shop()
    {
        // assert
        var schema = ComposeShoppingSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              users {
                nodes {
                  reviews {
                    nodes {
                      product {
                        deliveryEstimate(zip: "123")
                      }
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requirement_Directive_Leaks_Into_SourceSchema_Request_Shop()
    {
        // assert
        var schema = ComposeShoppingSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query findMe($skip: Boolean = true) {
              users {
                nodes {
                  id
                  birthdate
                  reviews {
                    nodes {
                      author {
                        id
                        name
                      }
                      product {
                        id

                        weight
                        deliveryEstimate(zip: "4383")
                        pictureFileName
                        price

                        quantity @skip(if: $skip)

                        reviews {
                          nodes {
                            author {
                              birthdate
                              reviews {
                                nodes {
                                  stars
                                  product {
                                    weight
                                    item {
                                       quantity
                                       product {
                                        name
                                       }
                                    }
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }
}
