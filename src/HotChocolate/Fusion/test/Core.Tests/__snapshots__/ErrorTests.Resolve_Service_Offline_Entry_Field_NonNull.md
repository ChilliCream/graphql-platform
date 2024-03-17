# Resolve_Service_Offline_Entry_Field_NonNull

## User Request

```graphql
{
  reviewById(id: "UmV2aWV3Cmkx")! {
    body
  }
}
```

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Subgraph Failure",
      "path": ["reviewById"]
    }
  ],
  "data": null
}
```

## QueryPlan

```json
{
  "document": "{ reviewById(id: \u0022UmV2aWV3Cmkx\u0022)! { body } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Reviews2",
        "document": "query fetch_reviewById_1 { reviewById(id: \u0022UmV2aWV3Cmkx\u0022) { body } }",
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
5E9ED7CB8D5E47CAA7DF59E9712C7BBE53C597E7
```

## Fusion Graph

```graphql
schema
  @fusion(version: 1)
  @transport(subgraph: "Reviews2", location: "http:\/\/localhost:5000\/graphql", kind: "HTTP")
  @transport(subgraph: "Reviews2", location: "ws:\/\/localhost:5000\/graphql", kind: "WebSocket")
  @node(subgraph: "Reviews2", types: [ "User", "Review" ]) {
  query: Query
  mutation: Mutation
  subscription: Subscription
}

type Query {
  "Fetches an object given its ID."
  node("ID of the object." id: ID!): Node
    @variable(subgraph: "Reviews2", name: "id", argument: "id")
    @resolver(subgraph: "Reviews2", select: "{ node(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  "Lookup nodes by a list of IDs."
  nodes("The list of node IDs." ids: [ID!]!): [Node]!
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
    @variable(subgraph: "Reviews2", name: "id", argument: "id")
    @resolver(subgraph: "Reviews2", select: "{ authorById(id: $id) }", arguments: [ { name: "id", type: "ID!" } ])
  viewer: Viewer!
    @resolver(subgraph: "Reviews2", select: "{ viewer }")
}

type Mutation {
  addReview(input: AddReviewInput!): AddReviewPayload!
    @variable(subgraph: "Reviews2", name: "input", argument: "input")
    @resolver(subgraph: "Reviews2", select: "{ addReview(input: $input) }", arguments: [ { name: "input", type: "AddReviewInput!" } ])
}

type Subscription {
  onNewReview: Review!
    @resolver(subgraph: "Reviews2", select: "{ onNewReview }", kind: "SUBSCRIBE")
}

type AddReviewPayload {
  review: Review
    @source(subgraph: "Reviews2")
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
  reviewsValue: String!
    @source(subgraph: "Reviews2")
}

"The user who wrote the review."
type User implements Node
  @variable(subgraph: "Reviews2", name: "User_id", select: "id")
  @resolver(subgraph: "Reviews2", select: "{ authorById(id: $User_id) }", arguments: [ { name: "User_id", type: "ID!" } ])
  @resolver(subgraph: "Reviews2", select: "{ nodes(ids: $User_id) { ... on User { ... User } } }", arguments: [ { name: "User_id", type: "[ID!]!" } ], kind: "BATCH") {
  id: ID!
    @source(subgraph: "Reviews2")
  name: String!
    @source(subgraph: "Reviews2")
  reviews: [Review!]!
    @source(subgraph: "Reviews2")
}

type Viewer {
  data: SomeData!
    @source(subgraph: "Reviews2")
  latestReview: Review
    @source(subgraph: "Reviews2")
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
```

