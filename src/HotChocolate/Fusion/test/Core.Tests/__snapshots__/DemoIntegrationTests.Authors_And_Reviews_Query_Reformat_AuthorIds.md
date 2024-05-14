# Authors_And_Reviews_Query_Reformat_AuthorIds

## User Request

```graphql
query ReformatIds {
  reviews {
    author {
      id
    }
  }
}
```

## Result

```json
{
  "data": {
    "reviews": [
      {
        "author": {
          "id": "VXNlcgppMQ=="
        }
      },
      {
        "author": {
          "id": "VXNlcgppMg=="
        }
      },
      {
        "author": {
          "id": "VXNlcgppMQ=="
        }
      },
      {
        "author": {
          "id": "VXNlcgppMg=="
        }
      }
    ]
  }
}
```

## QueryPlan

```json
{
  "document": "query ReformatIds { reviews { author { id } } }",
  "operation": "ReformatIds",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews",
        "document": "query ReformatIds_1 { reviews { author { id } } }",
        "selectionSetId": 0
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

## QueryPlan Hash

```text
9F1489CA5289059663ED412471F2C4B87F8A4911
```

## Fusion Graph

```graphql
schema
  @fusion(version: 1)
  @transport(subgraph: "Reviews", location: "http:\/\/localhost:5000\/graphql", kind: "HTTP")
  @transport(subgraph: "Reviews", location: "ws:\/\/localhost:5000\/graphql", kind: "WebSocket")
  @transport(subgraph: "Accounts", location: "http:\/\/localhost:5000\/graphql", kind: "HTTP")
  @transport(subgraph: "Accounts", location: "ws:\/\/localhost:5000\/graphql", kind: "WebSocket")
  @node(subgraph: "Reviews", types: [ "User", "Review" ])
  @node(subgraph: "Accounts", types: [ "User" ]) {
  query: Query
  mutation: Mutation
  subscription: Subscription
}

type Query {
  errorField: String
    @resolver(subgraph: "Accounts", select: "{ errorField }")
  "Fetches an object given its ID."
  node("ID of the object." id: ID!): Node
    @variable(subgraph: "Reviews", name: "id", argument: "id")
    @resolver(subgraph: "Reviews", select: "{ node(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
    @variable(subgraph: "Accounts", name: "id", argument: "id")
    @resolver(subgraph: "Accounts", select: "{ node(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  "Lookup nodes by a list of IDs."
  nodes("The list of node IDs." ids: [ID!]!): [Node]!
    @variable(subgraph: "Reviews", name: "ids", argument: "ids")
    @resolver(subgraph: "Reviews", select: "{ nodes(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
    @variable(subgraph: "Accounts", name: "ids", argument: "ids")
    @resolver(subgraph: "Accounts", select: "{ nodes(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
  productById(id: ID!): Product
    @variable(subgraph: "Reviews", name: "id", argument: "id")
    @resolver(subgraph: "Reviews", select: "{ productById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  reviewById(id: ID!): Review
    @variable(subgraph: "Reviews", name: "id", argument: "id")
    @resolver(subgraph: "Reviews", select: "{ reviewById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  reviewOrAuthor: ReviewOrAuthor!
    @resolver(subgraph: "Reviews", select: "{ reviewOrAuthor }")
  reviews: [Review!]!
    @resolver(subgraph: "Reviews", select: "{ reviews }")
  userById(id: ID!): User
    @variable(subgraph: "Reviews", name: "id", argument: "id")
    @resolver(subgraph: "Reviews", select: "{ authorById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
    @variable(subgraph: "Accounts", name: "id", argument: "id")
    @resolver(subgraph: "Accounts", select: "{ userById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  users: [User!]!
    @resolver(subgraph: "Accounts", select: "{ users }")
  usersById(ids: [ID!]!): [User!]!
    @variable(subgraph: "Accounts", name: "ids", argument: "ids")
    @resolver(subgraph: "Accounts", select: "{ usersById(ids: $ids) }", arguments: [ { name: "ids", type: "[ID!]!" } ])
  viewer: Viewer!
    @resolver(subgraph: "Accounts", select: "{ viewer }")
}

type Mutation {
  addReview(input: AddReviewInput!): AddReviewPayload!
    @variable(subgraph: "Reviews", name: "input", argument: "input")
    @resolver(subgraph: "Reviews", select: "{ addReview(input: $input) }", arguments: [ { name: "input", type: "AddReviewInput!" } ])
  addUser(input: AddUserInput!): AddUserPayload!
    @variable(subgraph: "Accounts", name: "input", argument: "input")
    @resolver(subgraph: "Accounts", select: "{ addUser(input: $input) }", arguments: [ { name: "input", type: "AddUserInput!" } ])
}

type Subscription {
  onNewReview: Review!
    @resolver(subgraph: "Reviews", select: "{ onNewReview }", kind: "SUBSCRIBE")
}

type AddReviewPayload {
  review: Review
    @source(subgraph: "Reviews")
}

type AddUserPayload {
  user: User
    @source(subgraph: "Accounts")
}

type Product
  @variable(subgraph: "Reviews", name: "Product_id", select: "id")
  @resolver(subgraph: "Reviews", select: "{ productById(id: $Product_id) }", arguments: [ { name: "Product_id", type: "ID!" } ]) {
  id: ID!
    @source(subgraph: "Reviews")
  reviews: [Review!]!
    @source(subgraph: "Reviews")
}

type Review implements Node
  @variable(subgraph: "Reviews", name: "Review_id", select: "id")
  @resolver(subgraph: "Reviews", select: "{ reviewById(id: $Review_id) }", arguments: [ { name: "Review_id", type: "ID!" } ])
  @resolver(subgraph: "Reviews", select: "{ nodes(ids: $Review_id) { ... on Review { ... Review } } }", arguments: [ { name: "Review_id", type: "[ID!]!" } ], kind: "BATCH") {
  author: User!
    @source(subgraph: "Reviews")
  body: String!
    @source(subgraph: "Reviews")
  id: ID!
    @source(subgraph: "Reviews")
  product: Product!
    @source(subgraph: "Reviews")
}

type SomeData {
  accountValue: String!
    @source(subgraph: "Accounts")
}

type User implements Node
  @source(subgraph: "Reviews", name: "Author")
  @variable(subgraph: "Reviews", name: "User_id", select: "id")
  @variable(subgraph: "Accounts", name: "User_id", select: "id")
  @resolver(subgraph: "Reviews", select: "{ authorById(id: $User_id) }", arguments: [ { name: "User_id", type: "ID!" } ])
  @resolver(subgraph: "Accounts", select: "{ userById(id: $User_id) }", arguments: [ { name: "User_id", type: "ID!" } ])
  @resolver(subgraph: "Accounts", select: "{ usersById(ids: $User_id) }", arguments: [ { name: "User_id", type: "[ID!]!" } ], kind: "BATCH")
  @resolver(subgraph: "Reviews", select: "{ nodes(ids: $User_id) { ... on User { ... User } } }", arguments: [ { name: "User_id", type: "[ID!]!" } ], kind: "BATCH") {
  birthdate: Date!
    @source(subgraph: "Accounts")
  errorField: String
    @source(subgraph: "Accounts")
  id: ID!
    @source(subgraph: "Reviews")
    @source(subgraph: "Accounts")
    @reEncodeId
  name: String!
    @source(subgraph: "Reviews")
    @source(subgraph: "Accounts")
  reviews: [Review!]!
    @source(subgraph: "Reviews")
  username: String!
    @source(subgraph: "Accounts")
}

type Viewer {
  data: SomeData!
    @source(subgraph: "Accounts")
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

