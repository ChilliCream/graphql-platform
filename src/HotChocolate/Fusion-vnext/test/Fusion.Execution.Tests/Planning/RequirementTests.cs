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
    public void Plan_Simple_Operation_1_Source_Schema_1()
    {
        // arrange
        var schema = ComposeSchema(
            """"
            schema {
              query: Query
              subscription: Subscription
            }

            type Brand {
              id: Int!
              name: String!
              products: [Product!]!
            }

            "Information about pagination in a connection."
            type PageInfo {
              "Indicates whether more edges exist following the set defined by the clients arguments."
              hasNextPage: Boolean!
              "Indicates whether more edges exist prior the set defined by the clients arguments."
              hasPreviousPage: Boolean!
              "When paginating backwards, the cursor to continue."
              startCursor: String
              "When paginating forwards, the cursor to continue."
              endCursor: String
            }

            type Product {
              id: Int!
              name: String!
              description: String
              price: Decimal!
              imageFileName: String
              typeId: Int!
              type: ProductType
              brandId: Int!
              brand: Brand
              availableStock: Int!
              restockThreshold: Int!
              maxStockThreshold: Int!
              onReorder: Boolean!
            }

            type ProductType {
              id: Int!
              name: String!
              products: [Product!]!
            }

            "A connection to a list of items."
            type ProductsConnection {
              "Information to aid in pagination."
              pageInfo: PageInfo!
              "A list of edges."
              edges: [ProductsEdge!]
              "A flattened list of the nodes."
              nodes: [Product!]
            }

            "An edge in a connection."
            type ProductsEdge {
              "A cursor for use in pagination."
              cursor: String!
              "The item at the end of the edge."
              node: Product!
            }

            type Query {
              brandById(id: Int!): Brand @lookup @cost(weight: "10")
              productById(id: Int!): Product @lookup @cost(weight: "10")
              products("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String "Returns the last _n_ elements from the list." last: Int "Returns the elements in the list that come before the specified cursor." before: String where: ProductFilterInput @cost(weight: "10")): ProductsConnection @listSize(assumedSize: 50, slicingArguments: [ "first", "last" ], slicingArgumentDefaultValue: 10, sizedFields: [ "edges", "nodes" ], requireOneSlicingArgument: false) @cost(weight: "10")
            }

            type Subscription {
              onBrandAdded: Brand! @cost(weight: "10")
            }

            input DecimalOperationFilterInput {
              eq: Decimal @cost(weight: "10")
              neq: Decimal @cost(weight: "10")
              in: [Decimal] @cost(weight: "10")
              nin: [Decimal] @cost(weight: "10")
              gt: Decimal @cost(weight: "10")
              ngt: Decimal @cost(weight: "10")
              gte: Decimal @cost(weight: "10")
              ngte: Decimal @cost(weight: "10")
              lt: Decimal @cost(weight: "10")
              nlt: Decimal @cost(weight: "10")
              lte: Decimal @cost(weight: "10")
              nlte: Decimal @cost(weight: "10")
            }

            input ProductFilterInput {
              and: [ProductFilterInput!]
              or: [ProductFilterInput!]
              price: DecimalOperationFilterInput
            }

            "The purpose of the `cost` directive is to define a `weight` for GraphQL types, fields, and arguments. Static analysis can use these weights when calculating the overall cost of a query or response."
            directive @cost("The `weight` argument defines what value to add to the overall cost for every appearance, or possible appearance, of a type, field, argument, etc." weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            "The purpose of the `@listSize` directive is to either inform the static analysis about the size of returned lists (if that information is statically available), or to point the analysis to where to find that information."
            directive @listSize("The `assumedSize` argument can be used to statically define the maximum length of a list returned by a field." assumedSize: Int "The `slicingArguments` argument can be used to define which of the field's arguments with numeric type are slicing arguments, so that their value determines the size of the list returned by that field. It may specify a list of multiple slicing arguments." slicingArguments: [String!] "The `slicingArgumentDefaultValue` argument can be used to define a default value for a slicing argument, which is used if the argument is not present in a query." slicingArgumentDefaultValue: Int "The `sizedFields` argument can be used to define that the value of the `assumedSize` argument or of a slicing argument does not affect the size of a list returned by a field itself, but that of a list returned by one of its sub-fields." sizedFields: [String!] "The `requireOneSlicingArgument` argument can be used to inform the static analysis that it should expect that exactly one of the defined slicing arguments is present in a query. If that is not the case (i.e., if none or multiple slicing arguments are present), the static analysis may throw an error." requireOneSlicingArgument: Boolean! = true) on FIELD_DEFINITION

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION

            directive @schemaName(value: String!) on SCHEMA

            "The `Decimal` scalar type represents a decimal floating-point number."
            scalar Decimal
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

            directive @internal on OBJECT | FIELD_DEFINITION

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION

            directive @require("The field selection map syntax." field: FieldSelectionMap!) on ARGUMENT_DEFINITION

            directive @schemaName(value: String!) on SCHEMA

            scalar FieldSelectionMap
            """");

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                nodes {
                  brand { name }
                  nameAndId
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }
}
