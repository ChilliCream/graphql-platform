using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class PlannerBehaviorTests : FusionTestBase
{
    [Fact]
    public void Fragments_Simple_Inline_Fragment()
    {
        // arrange
        var schema = CreateFragmentPlanningSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                price {
                  amount
                  currency
                }
                ... on Product {
                  isAvailable
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Fragments_Fragment_Spread()
    {
        // arrange
        var schema = CreateFragmentPlanningSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            fragment ProductInfo on Product {
              isAvailable
            }

            query {
              products {
                price {
                  amount
                  currency
                }
                ...ProductInfo
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Include_Skip_Basic_Include()
    {
        // arrange
        var schema = CreateIncludeSkipSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($include: Boolean) {
              product {
                price
                neverCalledInclude @include(if: $include)
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Include_Skip_Include_Fragment()
    {
        // arrange
        var schema = CreateIncludeSkipSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($include: Boolean) {
              product {
                price
                ... on Product @include(if: $include) {
                  neverCalledInclude
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Include_Skip_Basic_Skip()
    {
        // arrange
        var schema = CreateIncludeSkipSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($skip: Boolean = false) {
              product {
                price
                skip @skip(if: $skip)
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Include_Skip_Include_At_Root_Fetch()
    {
        // arrange
        var schema = CreateIncludeSkipSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($include: Boolean) {
              product {
                id
                price @include(if: $include)
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Include_Skip_Include_Fragment_At_Root_Fetch()
    {
        // arrange
        var schema = CreateIncludeSkipSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($include: Boolean) {
              product {
                id
                ... on Product @include(if: $include) {
                  price
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Root_Types_Shared_Root()
    {
        // arrange
        var schema = CreateSharedRootSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              product {
                id
                name {
                  id
                  brand
                  model
                }
                category {
                  id
                  name
                }
                price {
                  id
                  amount
                  currency
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Mutations()
    {
        // arrange
        var schema = CreateMutationPlanningSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation {
              addProduct(input: { name: "new", price: 599.99 }) {
                name
                price
                isExpensive
                isAvailable
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Mutations_Many_Fields_Two_Same_Graph()
    {
        // arrange
        var schema = CreateMutationPlanningSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation {
              five: add(num: 5)
              ten: multiply(by: 2)
              twelve: add(num: 2)
              final: delete
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Mutations_Many_Fields_Two_Same_Graph_Contiguous_Same_Service()
    {
        // arrange
        var schema = CreateMutationPlanningSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation {
              five: add(num: 5)
              seven: add(num: 2)
              fourteen: multiply(by: 2)
              sixteen: add(num: 2)
              final: delete
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateFragmentPlanningSchema()
    {
        return ComposeSchema(
            """
            # name: store
            schema {
              query: Query
            }

            type Query {
              products: [Product]
            }

            type Product @key(fields: "id") {
              id: ID! @shareable
              name: String!
            }
            """,
            """
            # name: info
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product
              @key(fields: "id") {
              id: ID! @shareable
              uuid: ID! @shareable
              isAvailable: Boolean!
            }
            """,
            """
            # name: cost
            schema {
              query: Query
            }

            type Query {
              productByUuid(uuid: ID! @is(field: "uuid")): Product @lookup @internal
            }

            type Product @key(fields: "uuid") {
              uuid: ID! @shareable
              price: Price
            }

            type Price {
              amount: Float!
              currency: String!
            }
            """);
    }

    private static FusionSchemaDefinition CreateIncludeSkipSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
              price: Float!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              isExpensive(price: Float! @require(field: "price")): Boolean!
            }
            """,
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              include(isExpensive: Boolean! @require(field: "isExpensive")): Boolean!
              skip(isExpensive: Boolean! @require(field: "isExpensive")): Boolean!
              neverCalledInclude(
                isExpensive: Boolean! @require(field: "isExpensive")): Boolean!
              neverCalledSkip(
                isExpensive: Boolean! @require(field: "isExpensive")): Boolean!
            }
            """);
    }

    private static FusionSchemaDefinition CreateSharedRootSchema()
    {
        return ComposeSchema(
            """
            # name: category
            schema {
              query: Query
            }

            type Query {
              product: Product! @shareable
            }

            type Product {
              id: ID!
              category: Category!
            }

            type Category {
              id: ID!
              name: String!
            }
            """,
            """
            # name: name
            schema {
              query: Query
            }

            type Query {
              product: Product! @shareable
            }

            type Product {
              name: Name!
            }

            type Name {
              id: ID!
              brand: String!
              model: String!
            }
            """,
            """
            # name: price
            schema {
              query: Query
            }

            type Query {
              product: Product! @shareable
            }

            type Product {
              price: Price!
            }

            type Price {
              id: ID!
              amount: Int!
              currency: String!
            }
            """);
    }

    private static FusionSchemaDefinition CreateMutationPlanningSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
              mutation: Mutation
            }

            type Query {
              product(id: ID!): Product!
              products: [Product!]!
            }

            type Mutation {
              addProduct(input: AddProductInput!): Product!
              multiply(by: Int!): Int!
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String!
              price: Float!
            }

            input AddProductInput {
              name: String!
              price: Float!
            }
            """,
            """
            # name: b
            schema {
              query: Query
              mutation: Mutation
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Mutation {
              delete: Int!
            }

            type Product @key(fields: "id") {
              id: ID!
              isExpensive(price: Float! @require(field: "price")): Boolean!
              isAvailable: Boolean!
            }
            """,
            """
            # name: c
            schema {
              query: Query
              mutation: Mutation
            }

            type Query {
              version: String
            }

            type Mutation {
              add(num: Int!): Int!
            }
            """);
    }
}
