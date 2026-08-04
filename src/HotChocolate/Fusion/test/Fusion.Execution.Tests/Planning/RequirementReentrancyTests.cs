using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class RequirementReentrancyTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_Reenter_Catalog_When_InnerProductCategory_Crosses_RequireBoundary()
    {
        // arrange
        var schema = CreateRecommendationSchema();

        // act
        // products (catalog) -> recommendations (recommendation service, @require "category")
        //   -> product.category (must re-enter the catalog service)
        var plan = PlanOperation(
            schema,
            """
            query getProducts {
              products {
                category
                recommendations {
                  product {
                    category
                  }
                }
              }
            }
            """);

        // assert
        // the recommendation service does not own Product.category, so its operation must
        // select only the inner product key (id) and a separate re-entrant catalog lookup
        // resolves product.category (target $.products.recommendations.product).
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  query getProducts {
                    products {
                      category
                      category @fusion__requirement
                      recommendations {
                        product {
                          category
                          id @fusion__requirement
                        }
                      }
                      id @fusion__requirement
                    }
                  }
                name: getProducts
                hash: 123456789101112
                searchSpace: 3
                expandedNodes: 9
            nodes:
              - id: 1
                type: Operation
                schema: CATALOG
                operation: |
                  query getProducts_123456789101112_1 {
                    products {
                      category
                      id
                    }
                  }
              - id: 3
                type: Operation
                schema: RECOMMENDATION
                operation: |
                  query getProducts_123456789101112_3(
                    $__fusion_2_category: ProductCategory!
                    $__fusion_3_id: ID!
                  ) {
                    productById(id: $__fusion_3_id) {
                      recommendations(category: $__fusion_2_category) {
                        product {
                          id
                        }
                      }
                    }
                  }
                source: $.productById
                target: $.products
                requirements:
                  - name: __fusion_2_category
                    selectionMap: >-
                      category
                  - name: __fusion_3_id
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
              - id: 4
                type: Operation
                schema: CATALOG
                operation: |
                  query getProducts_123456789101112_4($__fusion_4_id: ID!) {
                    productById(id: $__fusion_4_id) {
                      category
                    }
                  }
                source: $.productById
                target: $.products.recommendations.product
                requirements:
                  - name: __fusion_4_id
                    selectionMap: >-
                      id
                dependencies:
                  - id: 3
            """);
    }

    [Fact]
    public void Plan_Should_StayInRecommendation_When_InnerProduct_Selects_OnlyId()
    {
        // arrange
        var schema = CreateRecommendationSchema();

        // act
        // control: inner selection is `id`, already owned by the recommendation service,
        // so no re-entrant lookup back into the catalog is required.
        var plan = PlanOperation(
            schema,
            """
            query getProducts {
              products {
                category
                recommendations {
                  product {
                    id
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Reenter_Catalog_When_EnteringFromRecommendation_Standalone()
    {
        // arrange
        var schema = CreateRecommendationSchema();

        // act
        // control: enter from the recommendation service directly (no outer @require
        // boundary), product.category still needs a re-entrant lookup into the catalog.
        var plan = PlanOperation(
            schema,
            """
            query getProducts {
              recommendations {
                product {
                  category
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Resolve_User_When_Node_Entry_Crosses_Required_Field()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: PRODUCTS
            schema {
              query: Query
            }

            interface Node {
              id: ID!
            }

            type Query {
              node(id: ID!): Node @lookup @shareable
              productById(id: Int! @is(field: "productId")): Product @lookup @internal
            }

            type Product implements Node @key(fields: "productId") {
              productId: Int!
              id: ID!
              reviewAudience: String!
            }
            """,
            """
            # name: REVIEWS
            schema {
              query: Query
            }

            interface Node {
              id: ID!
            }

            type Query {
              node(id: ID!): Node @lookup @shareable
              productById(id: Int! @is(field: "productId")): Product @lookup @internal
            }

            type Product @key(fields: "productId") {
              productId: Int!
              reviews(
                audience: String! @require(field: "reviewAudience"))
                : [Review!]
            }

            type Review implements Node {
              id: ID!
              author: User
            }

            type User @key(fields: "userId") {
              userId: ID!
            }
            """,
            """
            # name: USERS
            schema {
              query: Query
            }

            type Query {
              userById(id: ID! @is(field: "userId"))
                : User @lookup @internal
            }

            type User @key(fields: "userId") {
              userId: ID!
              name: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query GetProduct($id: ID!) {
              node(id: $id) {
                ... on Product {
                  reviews {
                    author {
                      name
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
    public void Plan_Should_Resolve_User_When_Node_ConcreteFragment_IsConditional()
    {
        // arrange
        var schema = CreateRequiredFieldSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query GetProduct($id: ID!, $include: Boolean!) {
              node(id: $id) {
                ... on Product @include(if: $include) {
                  reviews {
                    author {
                      name
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
    public void Plan_Should_Resolve_User_When_Union_Entry_Crosses_Required_Field()
    {
        // arrange
        var schema = CreateRequiredFieldSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query SearchProduct {
              searchProduct {
                ... on ProductSearchResult {
                  product {
                    reviews {
                      author {
                        name
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
    public void Plan_Should_Inline_Separate_LookupRequirements_When_Targeting_Same_NodeBranch()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: PRODUCTS
            schema {
              query: Query
            }

            interface Node {
              id: ID!
            }

            type Query {
              node(id: ID!): Node @lookup @shareable
              productById(id: Int! @is(field: "productId")): Product @lookup @internal
            }

            type Product implements Node @key(fields: "productId") {
              productId: Int!
              id: ID!
              reviewAudience: String!
              recommendationAudience: String!
            }
            """,
            """
            # name: REVIEWS
            schema {
              query: Query
            }

            type Query {
              productById(id: Int! @is(field: "productId")): Product @lookup @internal
            }

            type Product @key(fields: "productId") {
              productId: Int!
              reviews(
                audience: String! @require(field: "reviewAudience"))
                : [Review!]
            }

            type Review {
              body: String
            }
            """,
            """
            # name: RECOMMENDATIONS
            schema {
              query: Query
            }

            type Query {
              productById(id: Int! @is(field: "productId")): Product @lookup @internal
            }

            type Product @key(fields: "productId") {
              productId: Int!
              recommendations(
                audience: String! @require(field: "recommendationAudience"))
                : [Recommendation!]
            }

            type Recommendation {
              text: String
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query GetProduct($id: ID!) {
              node(id: $id) {
                ... on Product {
                  reviews {
                    body
                  }
                  recommendations {
                    text
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Keep_Require_Subtree_In_Owning_Schema_When_Entry_Schema_Declares_Nested_Lookup()
    {
        // arrange
        var schema = CreateSupplierRatingSchema();

        // act
        // supplierBySlug (suppliers) -> rating (ratings, @require "_isEligibleForHighlyRecommended")
        //   -> review.booking.customer (must hop into the bookings service). The suppliers
        //   service also declares a Booking lookup, but owns neither Supplier.rating nor
        //   Booking.customer.
        var plan = PlanOperation(
            schema,
            """
            query GetSupplier {
              supplierBySlug(slug: "some-slug") {
                ... on Supplier {
                  rating {
                    reviews(first: 20) {
                      edges {
                        node {
                          booking {
                            customer {
                              firstName
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
        // the suppliers service must only resolve the entry field, the entity key and the
        // required field, the whole rating subtree belongs to the ratings service.
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  query GetSupplier {
                    supplierBySlug(slug: "some-slug") {
                      __typename @fusion__requirement
                      ... on Supplier {
                        rating {
                          reviews(first: 20) {
                            edges {
                              node {
                                booking {
                                  customer {
                                    firstName
                                  }
                                  dbId @fusion__requirement
                                }
                              }
                            }
                          }
                        }
                        dbId @fusion__requirement
                        _isEligibleForHighlyRecommended @fusion__requirement
                      }
                    }
                  }
                name: GetSupplier
                hash: 123456789101112
                searchSpace: 3
                expandedNodes: 9
            nodes:
              - id: 1
                type: Operation
                schema: SUPPLIERS
                operation: |
                  query GetSupplier_123456789101112_1 {
                    supplierBySlug(slug: "some-slug") {
                      __typename
                      ... on Supplier {
                        dbId
                        _isEligibleForHighlyRecommended
                      }
                    }
                  }
              - id: 3
                type: Operation
                schema: RATINGS
                operation: |
                  query GetSupplier_123456789101112_3(
                    $__fusion_2_isEligibleForHighlyRecommended: Boolean!
                    $__fusion_3_dbId: Int!
                  ) {
                    supplierByDbId(dbId: $__fusion_3_dbId) {
                      rating(
                        isEligibleForHighlyRecommended: $__fusion_2_isEligibleForHighlyRecommended
                      ) {
                        reviews(first: 20) {
                          edges {
                            node {
                              booking {
                                dbId
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                source: $.supplierByDbId
                target: $.supplierBySlug<Supplier>
                requirements:
                  - name: __fusion_2_isEligibleForHighlyRecommended
                    selectionMap: >-
                      _isEligibleForHighlyRecommended
                  - name: __fusion_3_dbId
                    selectionMap: >-
                      dbId
                dependencies:
                  - id: 1
              - id: 4
                type: Operation
                schema: BOOKINGS
                operation: |
                  query GetSupplier_123456789101112_4($__fusion_4_dbId: Int!) {
                    bookingByDbId(dbId: $__fusion_4_dbId) {
                      customer {
                        firstName
                      }
                    }
                  }
                source: $.bookingByDbId
                target: $.supplierBySlug<Supplier>.rating.reviews.edges.node.booking
                requirements:
                  - name: __fusion_4_dbId
                    selectionMap: >-
                      dbId
                dependencies:
                  - id: 3
            """);
    }

    private static FusionSchemaDefinition CreateRequiredFieldSchema()
        => ComposeSchema(
            """
            # name: PRODUCTS
            schema {
              query: Query
            }

            interface Node {
              id: ID!
            }

            type Query {
              node(id: ID!): Node @lookup @shareable
              searchProduct: SearchResult
              productById(id: Int! @is(field: "productId")): Product @lookup @internal
            }

            union SearchResult = ProductSearchResult

            type ProductSearchResult {
              product: Product!
            }

            type Product implements Node @key(fields: "productId") {
              productId: Int!
              id: ID!
              reviewAudience: String!
            }
            """,
            """
            # name: REVIEWS
            schema {
              query: Query
            }

            interface Node {
              id: ID!
            }

            type Query {
              node(id: ID!): Node @lookup @shareable
              productById(id: Int! @is(field: "productId")): Product @lookup @internal
            }

            type Product @key(fields: "productId") {
              productId: Int!
              reviews(
                audience: String! @require(field: "reviewAudience"))
                : [Review!]
            }

            type Review implements Node {
              id: ID!
              author: User
            }

            type User @key(fields: "userId") {
              userId: ID!
            }
            """,
            """
            # name: USERS
            schema {
              query: Query
            }

            type Query {
              userById(id: ID! @is(field: "userId"))
                : User @lookup @internal
            }

            type User @key(fields: "userId") {
              userId: ID!
              name: String
            }
            """);

    private static FusionSchemaDefinition CreateRecommendationSchema()
    {
        return ComposeSchema(
            """
            # name: CATALOG
            schema {
              query: Query
            }

            type Query {
              products: [Product!]
              productById(id: ID! @is(field: "id")): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              category: ProductCategory!
            }

            enum ProductCategory {
              ELECTRONICS
              BOOKS
            }
            """,
            """
            # name: RECOMMENDATION
            schema {
              query: Query
            }

            type Query {
              recommendations: [Recommendation!]
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              recommendations(
                category: ProductCategory! @require(field: "category")): [Recommendation!]
            }

            type Recommendation @key(fields: "id") {
              id: ID!
              product: Product
            }

            enum ProductCategory {
              ELECTRONICS
              BOOKS
            }
            """);
    }

    private static FusionSchemaDefinition CreateSupplierRatingSchema()
        => ComposeSchema(
            """
            # name: SUPPLIERS
            schema {
              query: Query
            }

            type Query {
              supplierBySlug(slug: String!): SupplierResult!
              supplierByDbId(dbId: Int! @is(field: "dbId")): Supplier @lookup @internal
              bookingByDbId(dbId: Int! @is(field: "dbId")): Booking @lookup @internal
            }

            union SupplierResult = Supplier | SupplierNotFound

            type SupplierNotFound {
              message: String!
            }

            type Supplier @key(fields: "dbId") {
              dbId: Int!
              name: String!
              _isEligibleForHighlyRecommended: Boolean!
            }

            type Booking @key(fields: "dbId") {
              dbId: Int!
            }
            """,
            """
            # name: RATINGS
            schema {
              query: Query
            }

            type Query {
              supplierByDbId(dbId: Int! @is(field: "dbId")): Supplier @lookup @internal
            }

            type Supplier @key(fields: "dbId") {
              dbId: Int!
              rating(
                isEligibleForHighlyRecommended: Boolean!
                  @require(field: "_isEligibleForHighlyRecommended")): Rating!
            }

            type Rating {
              average: Float!
              reviews(first: Int): ReviewConnection
            }

            type ReviewConnection {
              edges: [ReviewEdge!]
            }

            type ReviewEdge {
              node: Review!
            }

            type Review {
              booking: Booking
            }

            type Booking @key(fields: "dbId") {
              dbId: Int!
            }
            """,
            """
            # name: BOOKINGS
            schema {
              query: Query
            }

            type Query {
              bookingByDbId(dbId: Int! @is(field: "dbId")): Booking @lookup @internal
            }

            type Booking @key(fields: "dbId") {
              dbId: Int!
              customer: Customer
            }

            type Customer {
              firstName: String
            }
            """);
}
