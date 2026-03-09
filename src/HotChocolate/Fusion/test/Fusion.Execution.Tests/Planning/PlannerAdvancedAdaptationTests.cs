using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class PlannerAdvancedAdaptationTests : FusionTestBase
{
    [Fact]
    public void Provides_Simple_Provides()
    {
        // arrange
        var schema = CreateSimpleProvidesSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                reviews {
                  author {
                    username
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Provides_Nested_Provides()
    {
        // arrange
        var schema = CreateNestedProvidesSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                id
                categories {
                  id
                  name
                  details {
                    id
                    name
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requires_Provides_Simple_Requires_Provides()
    {
        // arrange
        var schema = CreateSimpleRequiresProvidesSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              me {
                reviews {
                  id
                  author {
                    id
                    username
                  }
                  product {
                    inStock
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Union_Union_Member_Mix()
    {
        // arrange
        var schema = CreateUnionIntersectionSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              media {
                __typename
                ... on Book {
                  title
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Union_Union_Overfetching_Test()
    {
        // arrange
        var schema = CreateUnionOverfetchingSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              review {
                ... on AnonymousReview {
                  product {
                    b
                  }
                }
                ... on UserReview {
                  product {
                    c
                    d
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Issues_Issue_281()
    {
        // arrange
        var schema = CreateIssue281Schema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              viewer {
                review {
                  ... on AnonymousReview {
                    __typename
                    product {
                      b
                    }
                  }
                  ... on UserReview {
                    __typename
                    product {
                      c
                      d
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
    public void Issues_Issue_190_With_Typename()
    {
        // arrange
        var schema = CreateIssue190Schema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($included: Boolean!) {
              recommender @include(if: $included) {
                id
                results {
                  ...Recommendable_Product
                  __typename
                }
                __typename
              }
            }

            fragment Recommendable_Product on Product {
              id
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Issues_Issue_190_Without_Typename()
    {
        // arrange
        var schema = CreateIssue190Schema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($included: Boolean!) {
              recommender @include(if: $included) {
                id
                results {
                  ...Recommendable_Product
                }
              }
            }

            fragment Recommendable_Product on Product {
              id
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Issues_Issue_190_Inline_Fragment()
    {
        // arrange
        var schema = CreateIssue190Schema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query ($included: Boolean!) {
              recommender @include(if: $included) {
                id
                results {
                  ... on Product {
                    id
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateSimpleProvidesSchema()
    {
        return ComposeSchema(
            """
            # name: products
            schema {
              query: Query
            }

            type Query {
              products: [Product]
            }

            type Product @key(fields: "upc") {
              upc: String!
              name: String
            }
            """,
            """
            # name: reviews
            schema {
              query: Query
            }

            type Query {
              productByUpc(upc: String! @is(field: "upc")): Product @lookup @internal
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type Product @key(fields: "upc") {
              upc: String!
              reviews: [Review]
            }

            type Review @key(fields: "id") {
              id: ID!
              author: User @provides(fields: "username")
            }

            type User @key(fields: "id") {
              id: ID!
              username: String @external
            }
            """,
            """
            # name: users
            schema {
              query: Query
            }

            type Query {
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type User @key(fields: "id") {
              id: ID!
              username: String
              name: String
            }
            """);
    }

    private static FusionSchemaDefinition CreateNestedProvidesSchema()
    {
        return ComposeSchema(
            """
            # name: all-products
            schema {
              query: Query
            }

            type Query {
              products: [Product]
            }

            type Product @key(fields: "id") {
              id: ID!
              categories: [Category]
                @provides(fields: "id name details { id name }")
            }

            type Category @key(fields: "id") {
              id: ID! @external
              name: String @external
              details: CategoryDetails @external
            }

            type CategoryDetails {
              id: ID! @external
              name: String @external
            }
            """,
            """
            # name: category
            schema {
              query: Query
            }

            type Query {
              categoryById(id: ID! @is(field: "id")): Category @lookup @internal
            }

            type Category @key(fields: "id") {
              id: ID!
              name: String
              details: CategoryDetails
            }

            type CategoryDetails {
              id: ID!
              name: String
            }
            """);
    }

    private static FusionSchemaDefinition CreateSimpleRequiresProvidesSchema()
    {
        return ComposeSchema(
            """
            # name: accounts
            schema {
              query: Query
            }

            type Query {
              me: User
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type User @key(fields: "id") {
              id: ID!
              username: String
              name: String
            }
            """,
            """
            # name: reviews
            schema {
              query: Query
            }

            type Query {
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type User @key(fields: "id") {
              id: ID!
              username: String @external
              reviews: [Review]
            }

            type Review @key(fields: "id") {
              id: ID!
              author: User @provides(fields: "username")
              product: Product
            }

            type Product @key(fields: "upc") {
              upc: String!
            }
            """,
            """
            # name: inventory
            schema {
              query: Query
            }

            type Query {
              productByUpc(upc: String! @is(field: "upc")): Product @lookup @internal
            }

            type Product @key(fields: "upc") {
              upc: String!
              inStock: Boolean
            }
            """);
    }

    private static FusionSchemaDefinition CreateUnionIntersectionSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              media: Media
            }

            union Media = Book | Song

            type Book @key(fields: "id") {
              id: ID!
              title: String! @shareable
              aTitle: String!
            }

            type Song @key(fields: "id") {
              id: ID!
              title: String!
              aTitle: String!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              bookById(id: ID! @is(field: "id")): Book @lookup @internal
              movieById(id: ID! @is(field: "id")): Movie @lookup @internal
            }

            union Media = Book | Movie

            type Book @key(fields: "id") {
              id: ID!
              title: String! @shareable
              bTitle: String!
            }

            type Movie @key(fields: "id") {
              id: ID!
              title: String!
              bTitle: String!
            }
            """);
    }

    private static FusionSchemaDefinition CreateUnionOverfetchingSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              review: Review
            }

            union Review = AnonymousReview | UserReview

            type UserReview {
              product: Product
            }

            type AnonymousReview {
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
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
              b: String!
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
              c: String!
            }
            """,
            """
            # name: d
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              d: String!
            }
            """);
    }

    private static FusionSchemaDefinition CreateIssue281Schema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              viewer: Viewer!
            }

            type Viewer {
              review: Review
            }

            union Review = AnonymousReview | UserReview

            type UserReview {
              product: Product
            }

            type AnonymousReview {
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
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

            type Product
              @key(fields: "id")
              @key(fields: "pid") {
              id: ID!
              pid: ID!
              b: String!
            }
            """,
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              productByPid(pid: ID! @is(field: "pid")): Product @lookup @internal
            }

            type Product @key(fields: "pid") {
              pid: ID!
              c: String!
            }
            """,
            """
            # name: d
            schema {
              query: Query
            }

            type Query {
              productByPid(pid: ID! @is(field: "pid")): Product @lookup @internal
            }

            type Product @key(fields: "pid") {
              pid: ID!
              d: String!
            }
            """);
    }

    private static FusionSchemaDefinition CreateIssue190Schema()
    {
        return ComposeSchema(
            """
            # name: recommender
            schema {
              query: Query
            }

            type Query {
              recommender: Recommender
            }

            type Recommender {
              id: ID!
              results: [Recommendable!]!
            }

            interface Recommendable {
              id: ID!
            }

            type Product implements Recommendable {
              id: ID!
            }
            """);
    }
}
