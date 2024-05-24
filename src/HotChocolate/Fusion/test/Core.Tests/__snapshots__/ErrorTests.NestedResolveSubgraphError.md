# NestedResolveSubgraphError

## User Request

```graphql
{
  reviewById(id: "UmV2aWV3OjE=") {
    body
    author {
      username
      errorField
    }
  }
}
```

## Result

```json
{
  "errors": [
    {
      "message": "SOME USER ERROR",
      "locations": [
        {
          "line": 1,
          "column": 101
        }
      ],
      "path": [
        "reviewById",
        "author",
        "errorField"
      ],
      "extensions": {
        "remotePath": [
          "userById",
          "errorField"
        ]
      }
    }
  ],
  "data": {
    "reviewById": {
      "body": "Love it!",
      "author": {
        "username": "@ada",
        "errorField": null
      }
    }
  }
}
```

## QueryPlan

```json
{
  "document": "{ reviewById(id: \u0022UmV2aWV3OjE=\u0022) { body author { username errorField } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews2",
        "document": "query fetch_reviewById_1 { reviewById(id: \u0022UmV2aWV3OjE=\u0022) { body author { __fusion_exports__1: id } } }",
        "selectionSetId": 0,
        "provides": [
          {
            "variable": "__fusion_exports__1"
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
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query fetch_reviewById_2($__fusion_exports__1: ID!) { userById(id: $__fusion_exports__1) { username errorField } }",
        "selectionSetId": 2,
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
        "type": "Compose",
        "selectionSetIds": [
          2
        ]
      }
    ]
  },
  "state": {
    "__fusion_exports__1": "User_id"
  }
}
```

## QueryPlan Hash

```text
7F7E32C4C0C896F19A72BEE33ED9FFBD051C91E6
```

## Fusion Graph

```graphql
schema
  @fusion(version: 1)
  @transport(subgraph: "Accounts", location: "http:\/\/localhost:5000\/graphql", kind: "HTTP")
  @transport(subgraph: "Accounts", location: "ws:\/\/localhost:5000\/graphql", kind: "WebSocket")
  @transport(subgraph: "Reviews2", location: "http:\/\/localhost:5000\/graphql", kind: "HTTP")
  @transport(subgraph: "Reviews2", location: "ws:\/\/localhost:5000\/graphql", kind: "WebSocket")
  @node(subgraph: "Accounts", types: [ "User" ])
  @node(subgraph: "Reviews2", types: [ "User", "Review" ]) {
  query: Query
  mutation: Mutation
  subscription: Subscription
}

type Query {
  errorField: String
    @resolver(subgraph: "Accounts", select: "{ errorField }")
  "Fetches an object given its ID."
  node("ID of the object." id: ID!): Node
    @variable(subgraph: "Accounts", name: "id", argument: "id")
    @resolver(subgraph: "Accounts", select: "{ node(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
    @variable(subgraph: "Reviews2", name: "id", argument: "id")
    @resolver(subgraph: "Reviews2", select: "{ node(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  "Lookup nodes by a list of IDs."
  nodes("The list of node IDs." ids: [ID!]!): [Node]!
    @variable(subgraph: "Accounts", name: "ids", argument: "ids")
    @resolver(subgraph: "Accounts", select: "{ nodes(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
    @variable(subgraph: "Reviews2", name: "ids", argument: "ids")
    @resolver(subgraph: "Reviews2", select: "{ nodes(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
  productById(id: ID!): Product
    @variable(subgraph: "Reviews2", name: "id", argument: "id")
    @resolver(subgraph: "Reviews2", select: "{ productById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  reviewById(id: ID!): Review
    @variable(subgraph: "Reviews2", name: "id", argument: "id")
    @resolver(subgraph: "Reviews2", select: "{ reviewById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  reviewOrAuthor: ReviewOrAuthor!
    @resolver(subgraph: "Reviews2", select: "{ reviewOrAuthor }")
  reviews: [Review!]!
    @resolver(subgraph: "Reviews2", select: "{ reviews }")
  userById(id: ID!): User
    @variable(subgraph: "Accounts", name: "id", argument: "id")
    @resolver(subgraph: "Accounts", select: "{ userById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
    @variable(subgraph: "Reviews2", name: "id", argument: "id")
    @resolver(subgraph: "Reviews2", select: "{ authorById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  users: [User!]!
    @resolver(subgraph: "Accounts", select: "{ users }")
  usersById(ids: [ID!]!): [User!]!
    @variable(subgraph: "Accounts", name: "ids", argument: "ids")
    @resolver(subgraph: "Accounts", select: "{ usersById(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
  viewer: Viewer!
    @resolver(subgraph: "Accounts", select: "{ viewer }")
    @resolver(subgraph: "Reviews2", select: "{ viewer }")
}

type Mutation {
  addReview(input: AddReviewInput!): AddReviewPayload!
    @variable(subgraph: "Reviews2", name: "input", argument: "input")
    @resolver(subgraph: "Reviews2", select: "{ addReview(input: $input) }", arguments: [ { name: "input", type: "AddReviewInput!" } ])
  addUser(input: AddUserInput!): AddUserPayload!
    @variable(subgraph: "Accounts", name: "input", argument: "input")
    @resolver(subgraph: "Accounts", select: "{ addUser(input: $input) }", arguments: [ { name: "input", type: "AddUserInput!" } ])
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

type Product
  @variable(subgraph: "Reviews2", name: "Product_id", select: "id")
  @resolver(subgraph: "Reviews2", select: "{ productById(id: $Product_id) }", arguments: [ { name: "Product_id", type: "ID!" } ]) {
  id: ID!
    @source(subgraph: "Reviews2")
  reviews: [Review!]!
    @source(subgraph: "Reviews2")
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
  reviewsValue: String!
    @source(subgraph: "Reviews2")
}

"The user who wrote the review."
type User implements Node
  @variable(subgraph: "Accounts", name: "User_id", select: "id")
  @variable(subgraph: "Reviews2", name: "User_id", select: "id")
  @resolver(subgraph: "Accounts", select: "{ userById(id: $User_id) }", arguments: [ { name: "User_id", type: "ID!" } ])
  @resolver(subgraph: "Accounts", select: "{ usersById(ids: $User_id) }", arguments: [ { name: "User_id", type: "[ID!]!" } ], kind: "BATCH")
  @resolver(subgraph: "Reviews2", select: "{ authorById(id: $User_id) }", arguments: [ { name: "User_id", type: "ID!" } ])
  @resolver(subgraph: "Reviews2", select: "{ nodes(ids: $User_id) { ... on User { ... User } } }", arguments: [ { name: "User_id", type: "[ID!]!" } ], kind: "BATCH") {
  birthdate: Date!
    @source(subgraph: "Accounts")
  errorField: String
    @source(subgraph: "Accounts")
  id: ID!
    @source(subgraph: "Accounts")
    @source(subgraph: "Reviews2")
  name: String!
    @source(subgraph: "Accounts")
    @source(subgraph: "Reviews2")
  reviews: [Review!]!
    @source(subgraph: "Reviews2")
  username: String!
    @source(subgraph: "Accounts")
}

type Viewer {
  data: SomeData!
    @source(subgraph: "Accounts")
    @source(subgraph: "Reviews2")
  latestReview: Review
    @source(subgraph: "Reviews2")
  user: User
    @source(subgraph: "Accounts")
}

"The node interface is implemented by entities that have a global unique identifier."
interface Node {
  id: ID!
}

union ReviewOrAuthor = User | Review

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

"The `Date` scalar represents an ISO-8601 compliant date type."
scalar Date
```

