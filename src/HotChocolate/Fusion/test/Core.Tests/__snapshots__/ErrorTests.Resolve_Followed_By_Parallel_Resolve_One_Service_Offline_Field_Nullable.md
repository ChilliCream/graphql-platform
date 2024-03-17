# Resolve_Followed_By_Parallel_Resolve_One_Service_Offline_Field_Nullable

## User Request

```graphql
{
  reviewById(id: "UmV2aWV3Cmkx")? {
    body
    author {
      username
    }
    product? {
      name
    }
  }
}
```

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Subgraph Failure",
      "path": [
        "reviewById",
        "product"
      ]
    }
  ],
  "data": {
    "reviewById": {
      "body": "Love it!",
      "author": {
        "username": "@ada"
      },
      "product": null
    }
  }
}
```

## QueryPlan

```json
{
  "document": "{ reviewById(id: \u0022UmV2aWV3Cmkx\u0022)? { body author { username } product? { name } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews2",
        "document": "query fetch_reviewById_1 { reviewById(id: \u0022UmV2aWV3Cmkx\u0022) { body author { __fusion_exports__1: id } product? { __fusion_exports__2: id } } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
          },
          {
            "variable": "__fusion_exports__2"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      },
      {
        "type": "Parallel",
        "nodes": [
          {
            "type": "Resolve",
            "subgraph": "Accounts",
            "document": "query fetch_reviewById_2($__fusion_exports__1: ID!) { userById(id: $__fusion_exports__1) { username } }",
            "selectionSetId": 3,
            "path": [
              "userById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__1"
              }
            ]
          },
          {
            "type": "Resolve",
            "subgraph": "Products",
            "document": "query fetch_reviewById_3($__fusion_exports__2: ID!) { productById(id: $__fusion_exports__2) { name } }",
            "selectionSetId": 2,
            "path": [
              "productById"
            ],
            "requires": [
              {
                "variable": "__fusion_exports__2"
              }
            ]
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          2,
          3
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id",
    "__fusion_exports__2": "Product_id"
  }
}
```

## QueryPlan Hash

```text
A001247A8CA5D43628D0FABF927204A29CBA5DFE
```

## Fusion Graph

```graphql
schema
  @fusion(version: 1)
  @transport(subgraph: "Reviews2", location: "http:\/\/localhost:5000\/graphql", kind: "HTTP")
  @transport(subgraph: "Reviews2", location: "ws:\/\/localhost:5000\/graphql", kind: "WebSocket")
  @transport(subgraph: "Accounts", location: "http:\/\/localhost:5000\/graphql", kind: "HTTP")
  @transport(subgraph: "Accounts", location: "ws:\/\/localhost:5000\/graphql", kind: "WebSocket")
  @transport(subgraph: "Products", location: "http:\/\/localhost:5000\/graphql", kind: "HTTP")
  @transport(subgraph: "Products", location: "ws:\/\/localhost:5000\/graphql", kind: "WebSocket")
  @node(subgraph: "Reviews2", types: [ "User", "Review" ])
  @node(subgraph: "Accounts", types: [ "User" ])
  @node(subgraph: "Products", types: [ "Product" ]) {
  query: Query
  mutation: Mutation
  subscription: Subscription
}

type Query {
  errorField: String
    @resolver(subgraph: "Accounts", select: "{ errorField }")
  "Fetches an object given its ID."
  node("ID of the object." id: ID!): Node
    @variable(subgraph: "Reviews2", name: "id", argument: "id")
    @resolver(subgraph: "Reviews2", select: "{ node(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
    @variable(subgraph: "Accounts", name: "id", argument: "id")
    @resolver(subgraph: "Accounts", select: "{ node(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
    @variable(subgraph: "Products", name: "id", argument: "id")
    @resolver(subgraph: "Products", select: "{ node(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  "Lookup nodes by a list of IDs."
  nodes("The list of node IDs." ids: [ID!]!): [Node]!
    @variable(subgraph: "Reviews2", name: "ids", argument: "ids")
    @resolver(subgraph: "Reviews2", select: "{ nodes(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
    @variable(subgraph: "Accounts", name: "ids", argument: "ids")
    @resolver(subgraph: "Accounts", select: "{ nodes(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
    @variable(subgraph: "Products", name: "ids", argument: "ids")
    @resolver(subgraph: "Products", select: "{ nodes(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
  productById(id: ID!): Product
    @variable(subgraph: "Reviews2", name: "id", argument: "id")
    @resolver(subgraph: "Reviews2", select: "{ productById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
    @variable(subgraph: "Products", name: "id", argument: "id")
    @resolver(subgraph: "Products", select: "{ productById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  reviewById(id: ID!): Review
    @variable(subgraph: "Reviews2", name: "id", argument: "id")
    @resolver(subgraph: "Reviews2", select: "{ reviewById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  reviewOrAuthor: ReviewOrAuthor!
    @resolver(subgraph: "Reviews2", select: "{ reviewOrAuthor }")
  reviews: [Review!]!
    @resolver(subgraph: "Reviews2", select: "{ reviews }")
  topProducts(first: Int!): [Product!]!
    @variable(subgraph: "Products", name: "first", argument: "first")
    @resolver(subgraph: "Products", select: "{ topProducts(first: $first) }", arguments: [ { name: "first", type: "Int!" } ])
  userById(id: ID!): User
    @variable(subgraph: "Reviews2", name: "id", argument: "id")
    @resolver(subgraph: "Reviews2", select: "{ authorById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
    @variable(subgraph: "Accounts", name: "id", argument: "id")
    @resolver(subgraph: "Accounts", select: "{ userById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  users: [User!]!
    @resolver(subgraph: "Accounts", select: "{ users }")
  usersById(ids: [ID!]!): [User!]!
    @variable(subgraph: "Accounts", name: "ids", argument: "ids")
    @resolver(subgraph: "Accounts", select: "{ usersById(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
  viewer: Viewer!
    @resolver(subgraph: "Reviews2", select: "{ viewer }")
    @resolver(subgraph: "Accounts", select: "{ viewer }")
}

type Mutation {
  addReview(input: AddReviewInput!): AddReviewPayload!
    @variable(subgraph: "Reviews2", name: "input", argument: "input")
    @resolver(subgraph: "Reviews2", select: "{ addReview(input: $input) }", arguments: [ { name: "input", type: "AddReviewInput!" } ])
  addUser(input: AddUserInput!): AddUserPayload!
    @variable(subgraph: "Accounts", name: "input", argument: "input")
    @resolver(subgraph: "Accounts", select: "{ addUser(input: $input) }", arguments: [ { name: "input", type: "AddUserInput!" } ])
  uploadProductPicture(input: UploadProductPictureInput!): UploadProductPicturePayload!
    @variable(subgraph: "Products", name: "input", argument: "input")
    @resolver(subgraph: "Products", select: "{ uploadProductPicture(input: $input) }", arguments: [ { name: "input", type: "UploadProductPictureInput!" } ])
}

type Subscription {
  onNewReview: Review!
    @resolver(subgraph: "Reviews2", select: "{ onNewReview }", kind: "SUBSCRIBE")
}

type AddReviewPayload {
  review: Review
    @source(subgraph: "Reviews2")
}

type AddUserPayload {
  user: User
    @source(subgraph: "Accounts")
}

type Product implements Node
  @variable(subgraph: "Reviews2", name: "Product_id", select: "id")
  @variable(subgraph: "Products", name: "Product_id", select: "id")
  @resolver(subgraph: "Reviews2", select: "{ productById(id: $Product_id) }", arguments: [ { name: "Product_id", type: "ID!" } ])
  @resolver(subgraph: "Products", select: "{ productById(id: $Product_id) }", arguments: [ { name: "Product_id", type: "ID!" } ])
  @resolver(subgraph: "Products", select: "{ nodes(ids: $Product_id) { ... on Product { ... Product } } }", arguments: [ { name: "Product_id", type: "[ID!]!" } ], kind: "BATCH") {
  dimension: ProductDimension!
    @source(subgraph: "Products")
  id: ID!
    @source(subgraph: "Reviews2")
    @source(subgraph: "Products")
  name: String!
    @source(subgraph: "Products")
  price: Int!
    @source(subgraph: "Products")
  repeat(num: Int!): Int!
    @source(subgraph: "Products")
    @variable(subgraph: "Products", name: "num", argument: "num")
  repeatData(data: SomeDataInput!): SomeData!
    @source(subgraph: "Products")
    @variable(subgraph: "Products", name: "data", argument: "data")
  reviews: [Review!]!
    @source(subgraph: "Reviews2")
  weight: Int!
    @source(subgraph: "Products")
}

type ProductDimension {
  size: Int!
    @source(subgraph: "Products")
  weight: Int!
    @source(subgraph: "Products")
}

type ProductNotFoundError implements Error {
  message: String!
    @source(subgraph: "Products")
  productId: Int!
    @source(subgraph: "Products")
}

type Review implements Node
  @variable(subgraph: "Reviews2", name: "Review_id", select: "id")
  @resolver(subgraph: "Reviews2", select: "{ reviewById(id: $Review_id) }", arguments: [ { name: "Review_id", type: "ID!" } ])
  @resolver(subgraph: "Reviews2", select: "{ nodes(ids: $Review_id) { ... on Review { ... Review } } }", arguments: [ { name: "Review_id", type: "[ID!]!" } ], kind: "BATCH") {
  author: User!
    @source(subgraph: "Reviews2")
  body: String!
    @source(subgraph: "Reviews2")
  errorField: String
    @source(subgraph: "Reviews2")
  id: ID!
    @source(subgraph: "Reviews2")
  product: Product!
    @source(subgraph: "Reviews2")
}

type SomeData {
  accountValue: String!
    @source(subgraph: "Accounts")
  data: SomeData
    @source(subgraph: "Products")
  num: Int
    @source(subgraph: "Products")
  reviewsValue: String!
    @source(subgraph: "Reviews2")
}

type UploadProductPicturePayload {
  boolean: Boolean
    @source(subgraph: "Products")
  errors: [UploadProductPictureError!]
    @source(subgraph: "Products")
}

"The user who wrote the review."
type User implements Node
  @variable(subgraph: "Reviews2", name: "User_id", select: "id")
  @variable(subgraph: "Accounts", name: "User_id", select: "id")
  @resolver(subgraph: "Reviews2", select: "{ authorById(id: $User_id) }", arguments: [ { name: "User_id", type: "ID!" } ])
  @resolver(subgraph: "Accounts", select: "{ userById(id: $User_id) }", arguments: [ { name: "User_id", type: "ID!" } ])
  @resolver(subgraph: "Accounts", select: "{ usersById(ids: $User_id) }", arguments: [ { name: "User_id", type: "[ID!]!" } ], kind: "BATCH")
  @resolver(subgraph: "Reviews2", select: "{ nodes(ids: $User_id) { ... on User { ... User } } }", arguments: [ { name: "User_id", type: "[ID!]!" } ], kind: "BATCH") {
  birthdate: Date!
    @source(subgraph: "Accounts")
  errorField: String
    @source(subgraph: "Accounts")
  id: ID!
    @source(subgraph: "Reviews2")
    @source(subgraph: "Accounts")
  name: String!
    @source(subgraph: "Reviews2")
    @source(subgraph: "Accounts")
  reviews: [Review!]!
    @source(subgraph: "Reviews2")
  username: String!
    @source(subgraph: "Accounts")
}

type Viewer {
  data: SomeData!
    @source(subgraph: "Reviews2")
    @source(subgraph: "Accounts")
  latestReview: Review
    @source(subgraph: "Reviews2")
  user: User
    @source(subgraph: "Accounts")
}

interface Error {
  message: String!
}

"The node interface is implemented by entities that have a global unique identifier."
interface Node {
  id: ID!
}

union ReviewOrAuthor = User | Review

union UploadProductPictureError = ProductNotFoundError

input AddReviewInput {
  authorId: Int!
  body: String!
  upc: Int!
}

input AddUserInput {
  birthdate: Date!
  name: String!
  username: String!
}

input SomeDataInput {
  data: SomeDataInput
  num: Int
}

input UploadProductPictureInput {
  file: Upload!
  productId: Int!
}

"The `Date` scalar represents an ISO-8601 compliant date type."
scalar Date

"The `Upload` scalar type represents a file upload."
scalar Upload
```

