namespace HotChocolate.Fusion.Planning;

public class NodeLookupTests : FusionTestBase
{
    [Fact]
    public void Requirement_SelectionMap_Object_Shop()
    {
        // arrange
        var schema = ComposeSchema(
            """"
            interface Node {
              id: ID!
            }

            type PageInfo {
              hasNextPage: Boolean!
              hasPreviousPage: Boolean!
              startCursor: String
              endCursor: String
            }

            type Query {
              node("ID of the object." id: ID!): Node @lookup
              nodes("The list of node IDs." ids: [ID!]!): [Node]!
              userById(id: ID!): User @lookup
              userByUsername(username: String!): User @lookup
              users(first: Int after: String last: Int before: String): UsersConnection
            }

            type User implements Node {
              id: ID!
              name: String!
              birthdate: String!
              username: String!
            }

            type UsersConnection {
              pageInfo: PageInfo!
              edges: [UsersEdge!]
              nodes: [User!]
            }

            type UsersEdge {
              cursor: String!
              node: User!
            }
            """",
            """"
            interface Node {
              id: ID!
            }

            type InventoryItem implements Node {
              product: Product!
              id: ID!
              quantity: Int!
            }

            type Mutation {
              restockProduct(input: RestockProductInput!): RestockProductPayload! @cost(weight: "10")
            }

            type Product {
              quantity: Int! @cost(weight: "10")
              id: ID!
              item: InventoryItem
            }

            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
              inventoryItemById(id: ID!): InventoryItem @lookup
              productByIdAsync(id: ID!): Product @lookup @internal
            }

            type RestockProductPayload {
              product: Product
            }

            input RestockProductInput {
              id: ID!
              quantity: Int!
            }
            """",
            """"
            interface Node {
              id: ID!
            }

            type CreateOrderPayload {
              order: Order
            }

            type Mutation {
              createOrder(input: CreateOrderInput!): CreateOrderPayload! @cost(weight: "10")
            }

            type Order implements Node {
              user: User!
              id: ID!
              items: [OrderItem!]!
              weight: Int!
            }

            type OrderItem {
              product: Product!
              id: Int!
              quantity: Int!
              price: Float!
              orderId: Int!
              order: Order
            }

            type Product {
              id: ID!
            }

            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
              orderById(id: ID!): Order @lookup
              userById(id: ID!): User! @lookup @internal
            }

            type User {
              id: ID!
            }

            input CreateOrderInput {
              userId: ID!
              items: [OrderItemInput!]!
              weight: Int!
            }

            input OrderItemInput {
              productId: ID!
              quantity: Int!
              price: Float!
            }
            """",
            """"
            "The node interface is implemented by entities that have a global unique identifier."
            interface Node {
              id: ID!
            }

            type CreatePaymentPayload {
              payment: Payment
            }

            type Mutation {
              createPayment(input: CreatePaymentInput!): CreatePaymentPayload!
            }

            type Order {
              payments: [Payment!]!
              id: ID!
            }

            type Payment implements Node {
              order: Order!
              id: ID!
              amount: Float!
              status: PaymentStatus!
              createdAt: String!
            }

            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
              paymentById(id: ID!): Payment @lookup
              orderById(id: ID!): Order! @lookup
            }

            input CreatePaymentInput {
              orderId: ID!
            }

            enum PaymentStatus {
              PENDING
              AUTHORIZED
              DECLINED
              REFUNDED
            }
            """",
            """"
            interface Error {
              message: String!
            }

            interface Node {
              id: ID!
            }

            type Mutation {
              uploadProductPicture(input: UploadProductPictureInput!): UploadProductPicturePayload!
            }

            type PageInfo {
              hasNextPage: Boolean!
              hasPreviousPage: Boolean!
              startCursor: String
              endCursor: String
            }

            type Product implements Node {
              dimension: ProductDimension!
              pictureString: String
              id: ID!
              name: String!
              price: Float!
              weight: Int!
              pictureFileName: String
            }

            type ProductDimension {
              length: Float!
              width: Float!
              height: Float!
            }

            type ProductsConnection {
              pageInfo: PageInfo!
              edges: [ProductsEdge!]
              nodes: [Product!]
            }

            type ProductsEdge {
              cursor: String!
              node: Product!
            }

            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
              productById(id: ID!): Product @lookup
              products(first: Int after: String last: Int before: String): ProductsConnection
            }

            type UnknownProductError implements Error {
              productId: Int!
              message: String!
            }

            type UploadProductPicturePayload {
              product: Product
              errors: [UploadProductPictureError!]
            }

            union UploadProductPictureError = UnknownProductError

            input UploadProductPictureInput {
              productId: Int!
              picture: String!
            }
            """",
            """"
            interface Node {
              id: ID!
            }

            type CreateReviewPayload {
              review: Review
            }

            type Mutation {
              createReview(input: CreateReviewInput!): CreateReviewPayload!
            }

            type PageInfo {
              hasNextPage: Boolean!
              hasPreviousPage: Boolean!
              startCursor: String
              endCursor: String
            }

            type Product {
              reviews(first: Int after: String last: Int before: String): ProductReviewsConnection
              id: ID!
            }

            type ProductReviewsConnection {
              pageInfo: PageInfo!
              edges: [ProductReviewsEdge!]
              nodes: [Review!]
            }

            type ProductReviewsEdge {
              cursor: String!
              node: Review!
            }

            type Query {
              node(id: ID!): Node @lookup
              nodes(ids: [ID!]!): [Node]!
              productById(id: ID!): Product! @lookup @internal
              reviewById(id: ID!): Review @lookup
              userById(id: ID!): User @lookup @internal
            }

            type Review implements Node {
              product: Product!
              author: User
              id: ID!
              body: String!
              stars: Int!
              createAt: String!
            }

            type Subscription {
              onCreateReview: Review
            }

            type User {
              reviews(first: Int after: String last: Int before: String): UserReviewsConnection
              id: ID!
              name: String!
            }

            type UserReviewsConnection {
              pageInfo: PageInfo!
              edges: [UserReviewsEdge!]
              nodes: [Review!]
            }

            type UserReviewsEdge {
              cursor: String!
              node: Review!
            }

            input CreateReviewInput {
              body: String!
              stars: Int!
              productId: ID!
              authorId: ID!
            }
            """",
            """"
            type Product {
              deliveryEstimate(
                zip: String!
                dimension: ProductDimensionInput!
                  @require(field:
                    """
                    {
                      weight
                      length: dimension.length
                      width: dimension.width
                      height: dimension.height
                    }
                    """)): Int!
              id: ID!
            }

            type Query {
              productById(id: ID!): Product! @lookup @internal
            }

            input ProductDimensionInput {
              weight: Int!
              length: Float!
              width: Float!
              height: Float!
            }
            """");

        // act
        var plan = PlanOperation(
            schema,
            """
            query findMe {
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
                        quantity
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
